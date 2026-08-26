using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;
using TCC_SistemaEmpresa.Validation;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE")]
    public class FuncionariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FuncionariosController> _logger;

        public FuncionariosController(AppDbContext context, ILogger<FuncionariosController> logger)
        {
            _context = context;
            _logger = logger;
        }


        [HttpGet]
        public async Task<IActionResult> Index(string? busca, string? situacao)
        {
            var empresaId = EmpresaIdAtual();
            situacao = NormalizarSituacao(situacao);

            var consulta = _context.Funcionarios
                .AsNoTracking()
                .Where(f => f.EmpresaId == empresaId);

            consulta = situacao switch
            {
                SituacaoFiltro.Ativos => consulta.Where(f => f.Ativo),
                SituacaoFiltro.Inativos => consulta.Where(f => !f.Ativo),
                _ => consulta
            };
 
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                var digitos = CpfAttribute.ApenasDigitos(termo);


                consulta = digitos.Length > 0
                    ? consulta.Where(f => f.Nome.Contains(termo) || f.Cpf.Contains(digitos))
                    : consulta.Where(f => f.Nome.Contains(termo));
            }

            var funcionarios = await consulta
                .OrderBy(f => f.Nome)
                .Select(f => new FuncionarioLinhaViewModel
                {
                    Id = f.Id,
                    Nome = f.Nome,
                    Cargo = f.Cargo.Nome,
                    Salario = f.Salario ?? f.Cargo.SalarioBase,
                    Ativo = f.Ativo
                })
                .ToListAsync();

            var ids = funcionarios.Select(f => f.Id).ToList();

            var vendasPorFuncionario = await _context.Vendas
                .AsNoTracking()
                .Where(v => v.EmpresaId == empresaId && ids.Contains(v.FuncionarioId))
                .GroupBy(v => v.FuncionarioId)
                .Select(grupo => new { FuncionarioId = grupo.Key, Total = grupo.Count() })
                .ToDictionaryAsync(x => x.FuncionarioId, x => x.Total);

            foreach (var funcionario in funcionarios)
            {
                funcionario.QuantidadeVendas =
                    vendasPorFuncionario.TryGetValue(funcionario.Id, out var total) ? total : 0;
            }

            return View(new FuncionarioListaViewModel
            {
                Busca = busca,
                Situacao = situacao,
                Funcionarios = funcionarios
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var funcionario = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (funcionario is null)
                return NotFound();

            var model = ParaFormulario(funcionario);
            model.SomenteLeitura = true;
            model.Cargos = await CarregarCargosAsync(funcionario.CargoId);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new FuncionarioFormViewModel
            {
                Ativo = true,
                DataAdmissao = DateTime.Today,
                ProximoId = await ProximoIdAsync(),
                Cargos = await CarregarCargosAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FuncionarioFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();
            var cpf = CpfAttribute.ApenasDigitos(model.Cpf);

            await ValidarRegrasAsync(model, cpf, empresaId, funcionarioId: null);

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                model.Cargos = await CarregarCargosAsync(model.CargoId);
                return View(model);
            }

            var funcionario = new Funcionario
            {
                EmpresaId = empresaId,
                CargoId = model.CargoId,
                Nome = model.Nome.Trim(),
                Cpf = cpf,
                Salario = model.Salario,
                PerComissao = model.PerComissao,
                DataAdmissao = model.DataAdmissao.Date,
                Ativo = model.Ativo
            };

            await using var transacao = await _context.Database.BeginTransactionAsync();

            _context.Funcionarios.Add(funcionario);
            await _context.SaveChangesAsync();

            RegistrarLog("CRIACAO", funcionario.Id,
                $"Funcionário '{funcionario.Nome}' criado (cargo {funcionario.CargoId}).");
            await _context.SaveChangesAsync();

            await transacao.CommitAsync();

            _logger.LogInformation(
                "Funcionário {FuncionarioId} criado na empresa {EmpresaId}.",
                funcionario.Id, empresaId);

            TempData["Sucesso"] = $"Funcionário {funcionario.Nome} cadastrado com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var funcionario = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (funcionario is null)
                return NotFound();

            var model = ParaFormulario(funcionario);
            model.Cargos = await CarregarCargosAsync(funcionario.CargoId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, FuncionarioFormViewModel model)
        {
            var funcionario = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (funcionario is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();
            var cpf = CpfAttribute.ApenasDigitos(model.Cpf);

            await ValidarRegrasAsync(model, cpf, empresaId, funcionarioId: id);

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.Cargos = await CarregarCargosAsync(model.CargoId);
                return View(model);
            }
            var estavaAtivo = funcionario.Ativo;

            funcionario.CargoId = model.CargoId;
            funcionario.Nome = model.Nome.Trim();
            funcionario.Cpf = cpf;
            funcionario.Salario = model.Salario;
            funcionario.PerComissao = model.PerComissao;
            funcionario.DataAdmissao = model.DataAdmissao.Date;
            funcionario.Ativo = model.Ativo;


            var (acao, detalhe) = (estavaAtivo, model.Ativo) switch
            {
                (true, false) => ("INATIVACAO", "inativado"),
                (false, true) => ("REATIVACAO", "reativado"),
                _ => ("ALTERACAO", "alterado")
            };


            RegistrarLog(acao, funcionario.Id, $"Funcionário '{funcionario.Nome}' {detalhe}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Funcionário {FuncionarioId} da empresa {EmpresaId}: {Acao}.",
                funcionario.Id, empresaId, acao);

            TempData["Sucesso"] = $"Funcionário {funcionario.Nome} {detalhe} com sucesso.";
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromRoute] int id)
        {
            var funcionario = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (funcionario is null)
                return NotFound();

            var nome = funcionario.Nome;

            if (funcionario.Ativo)
            {
                TempData["Erro"] = $"O funcionário {nome} precisa ser inativado antes de ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            var quantidadeVendas = await _context.Vendas
                .AsNoTracking()
                .CountAsync(v => v.FuncionarioId == id);

            if (quantidadeVendas > 0)
            {
                TempData["Erro"] =
                    $"O funcionário {nome} possui {quantidadeVendas} venda(s) vinculada(s) e não pode ser excluído.";
                return RedirectToAction(nameof(Index));
            }
            RegistrarLog("EXCLUSAO", funcionario.Id,
                $"Funcionário '{nome}' (CPF {funcionario.Cpf}) excluído definitivamente.");

            _context.Funcionarios.Remove(funcionario);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Funcionário {FuncionarioId} ({Nome}) excluído definitivamente por {Usuario}.",
                id, nome, User.Identity?.Name);

            TempData["Sucesso"] = $"Funcionário {nome} foi excluído definitivamente.";
            return RedirectToAction(nameof(Index));
        }
        private Task<Funcionario?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.Funcionarios.AsTracking()
                : _context.Funcionarios.AsNoTracking();

            return consulta.FirstOrDefaultAsync(f => f.Id == id && f.EmpresaId == empresaId);
        }
        private async Task ValidarRegrasAsync(
            FuncionarioFormViewModel model, string cpf, int empresaId, int? funcionarioId)
        {
            if (model.CargoId > 0)
            {
                var cargoValido = await _context.Cargo
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == model.CargoId && c.EmpresaId == empresaId);

                if (!cargoValido)
                    ModelState.AddModelError(nameof(model.CargoId), "Cargo inválido.");
            }


            if (cpf.Length == 11)
            {
                var cpfEmUso = await _context.Funcionarios
                    .AsNoTracking()
                    .AnyAsync(f => f.EmpresaId == empresaId
                                && f.Cpf == cpf
                                && (funcionarioId == null || f.Id != funcionarioId));

                if (cpfEmUso)
                    ModelState.AddModelError(nameof(model.Cpf),
                        "Já existe um funcionário com este CPF nesta empresa.");
            }
        }

        private async Task<int?> ProximoIdAsync()
        {
            const string sql = @"
                SELECT CASE
                           WHEN coluna.last_value IS NULL THEN CONVERT(int, coluna.seed_value)
                           ELSE CONVERT(int, coluna.last_value) + CONVERT(int, coluna.increment_value)
                       END AS Value
                  FROM sys.identity_columns AS coluna
                 WHERE coluna.object_id = OBJECT_ID('Tb_Funcionario')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao,
                    "Não foi possível prever o próximo id de Tb_Funcionario.");
                return null;
            }
        }

        private async Task<IEnumerable<SelectListItem>> CarregarCargosAsync(int? selecionado = null)
        {
            var empresaId = EmpresaIdAtual();

            var cargos = await _context.Cargo
                .AsNoTracking()
                .Where(c => c.EmpresaId == empresaId && c.Ativo)
                .OrderBy(c => c.Nome)
                .Select(c => new { c.Id, c.Nome })
                .ToListAsync();

            return cargos.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Nome,
                Selected = selecionado.HasValue && c.Id == selecionado.Value
            });
        }

        private static FuncionarioFormViewModel ParaFormulario(Funcionario funcionario) => new()
        {
            Id = funcionario.Id,
            Nome = funcionario.Nome,
            Cpf = funcionario.Cpf,
            Ativo = funcionario.Ativo,
            Salario = funcionario.Salario,
            PerComissao = funcionario.PerComissao,
            DataAdmissao = funcionario.DataAdmissao,
            CargoId = funcionario.CargoId
        };
        private void RegistrarLog(string acao, int registroId, string detalhes)
        {
            _context.LogsSistema.Add(new LogSistema
            {
                EmpresaId = EmpresaIdAtual(),
                UsuarioId = UsuarioIdAtual(),
                Acao = acao,
                EntidadeAfetada = nameof(Funcionario),
                RegistroId = registroId,
                DataHora = DateTime.Now,
                Detalhes = detalhes
            });
        }

        private static string NormalizarSituacao(string? situacao) => situacao switch
        {
            SituacaoFiltro.Ativos => SituacaoFiltro.Ativos,
            SituacaoFiltro.Inativos => SituacaoFiltro.Inativos,
            _ => SituacaoFiltro.Todos
        };

        private int EmpresaIdAtual()
        {
            var claim = User.FindFirstValue(Security.ClaimsEmpresa.EmpresaId);
            return int.TryParse(claim, out var empresaId) ? empresaId : 0;
        }

        private int? UsuarioIdAtual()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var usuarioId) ? usuarioId : null;
        }
    }
}
