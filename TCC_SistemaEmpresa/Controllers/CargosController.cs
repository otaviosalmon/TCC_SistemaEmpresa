using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE")]
    public class CargosController : ControllerValidacao
    {
        private readonly ILogger<CargosController> _logger;

        public CargosController(AppDbContext context, ILogger<CargosController> logger) : base(context)
        {
            _logger = logger;
        }

        protected override string EntidadeLog => nameof(Cargo);

        [HttpGet]
        public async Task<IActionResult> Index(string? busca, string? situacao, int pagina = 1)
        {
            var empresaId = EmpresaIdAtual();
            situacao = NormalizarSituacao(situacao);

            var consulta = _context.Cargo
                .AsNoTracking()
                .Where(c => c.EmpresaId == empresaId);

            consulta = situacao switch
            {
                SituacaoFiltro.Ativos => consulta.Where(c => c.Ativo),
                SituacaoFiltro.Inativos => consulta.Where(c => !c.Ativo),
                _ => consulta
            };

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(c => c.Nome.Contains(termo));
            }

            var paginacao = PaginacaoViewModel.Criar(pagina, await consulta.CountAsync());

            var cargos = await consulta
                .OrderBy(c => c.Nome)
                .Pagina(paginacao)
                .Select(c => new CargoLinhaViewModel
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    SalarioBase = c.SalarioBase,
                    PerComissaoBase = c.PerComissaoBase,
                    Ativo = c.Ativo
                })
                .ToListAsync();

            await PreencherVinculosAsync(cargos, empresaId);

            return View(new CargoListaViewModel
            {
                Busca = busca,
                Situacao = situacao,
                Paginacao = paginacao,
                Cargos = cargos
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var cargo = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (cargo is null)
                return NotFound();

            var model = ParaFormulario(cargo);
            model.SomenteLeitura = true;
            await PreencherContagensAsync(model);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CargoFormViewModel
            {
                Ativo = true,
                ProximoId = await ProximoIdAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CargoFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();
            var nome = (model.Nome ?? string.Empty).Trim();

            await ValidarRegrasAsync(model, nome, empresaId, cargoId: null);

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                return View(model);
            }

            var cargo = new Cargo
            {
                EmpresaId = empresaId,
                Nome = nome,
                Descricao = TextoOuNulo(model.Descricao),
                SalarioBase = model.SalarioBase,
                PerComissaoBase = model.PerComissaoBase,
                Ativo = model.Ativo
            };

            await using var transacao = await _context.Database.BeginTransactionAsync();

            _context.Cargo.Add(cargo);
            await _context.SaveChangesAsync();

            RegistrarLog("CRIACAO", cargo.Id,
                $"Cargo '{cargo.Nome}' criado (salário base {FormatarSalario(cargo.SalarioBase)}, " +
                $"comissão base {FormatarComissao(cargo.PerComissaoBase)}).");
            await _context.SaveChangesAsync();

            await transacao.CommitAsync();

            _logger.LogInformation(
                "Cargo {CargoId} criado na empresa {EmpresaId}.",
                cargo.Id, empresaId);

            TempData["Sucesso"] = $"Cargo {cargo.Nome} cadastrado com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var cargo = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (cargo is null)
                return NotFound();

            var model = ParaFormulario(cargo);
            await PreencherContagensAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, CargoFormViewModel model)
        {
            var cargo = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (cargo is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();
            var nome = (model.Nome ?? string.Empty).Trim();

            var nomeOriginal = cargo.Nome;
            var estavaAtivo = cargo.Ativo;
            var salarioOriginal = cargo.SalarioBase;
            var comissaoOriginal = cargo.PerComissaoBase;

            await ValidarRegrasAsync(model, nome, empresaId, cargoId: id);

            if (estavaAtivo && !model.Ativo)
            {
                var funcionariosAtivos = await ContarFuncionariosAsync(id, somenteAtivos: true);

                if (funcionariosAtivos > 0)
                {
                    ModelState.AddModelError(nameof(model.Ativo),
                        $"Este cargo possui {funcionariosAtivos} funcionário(s) ativo(s). " +
                        "Transfira-os para outro cargo ou inative-os antes de inativar o cargo.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.Id = id;
                await PreencherContagensAsync(model);
                return View(model);
            }

            cargo.Nome = nome;
            cargo.Descricao = TextoOuNulo(model.Descricao);
            cargo.SalarioBase = model.SalarioBase;
            cargo.PerComissaoBase = model.PerComissaoBase;
            cargo.Ativo = model.Ativo;

            var (acao, detalhe) = (estavaAtivo, model.Ativo) switch
            {
                (true, false) => ("INATIVACAO", "inativado"),
                (false, true) => ("REATIVACAO", "reativado"),
                _ => ("ALTERACAO", "alterado")
            };

            var complemento = new List<string>();

            if (!string.Equals(nomeOriginal, nome, StringComparison.Ordinal))
                complemento.Add($"nome alterado de '{nomeOriginal}' para '{nome}'");

            if (salarioOriginal != model.SalarioBase)
                complemento.Add($"salário base de {FormatarSalario(salarioOriginal)} para {FormatarSalario(model.SalarioBase)}");

            if (comissaoOriginal != model.PerComissaoBase)
                complemento.Add($"comissão base de {FormatarComissao(comissaoOriginal)} para {FormatarComissao(model.PerComissaoBase)}");

            var textoComplemento = complemento.Count == 0
                ? string.Empty
                : " (" + string.Join("; ", complemento) + ")";

            RegistrarLog(acao, cargo.Id, $"Cargo '{nome}' {detalhe}{textoComplemento}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cargo {CargoId} da empresa {EmpresaId}: {Acao}.",
                cargo.Id, empresaId, acao);

            TempData["Sucesso"] = $"Cargo {nome} {detalhe} com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromRoute] int id)
        {
            var cargo = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (cargo is null)
                return NotFound();

            var nome = cargo.Nome;

            if (cargo.Ativo)
            {
                TempData["Erro"] = $"O cargo {nome} precisa ser inativado antes de ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            var quantidadeFuncionarios = await ContarFuncionariosAsync(id, somenteAtivos: false);

            if (quantidadeFuncionarios > 0)
            {
                TempData["Erro"] =
                    $"O cargo {nome} está vinculado a {quantidadeFuncionarios} funcionário(s) e não pode ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            RegistrarLog("EXCLUSAO", cargo.Id, $"Cargo '{nome}' excluído definitivamente.");

            _context.Cargo.Remove(cargo);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Cargo {CargoId} ({Nome}) excluído definitivamente por {Usuario}.",
                id, nome, User.Identity?.Name);

            TempData["Sucesso"] = $"Cargo {nome} foi excluído definitivamente.";
            return RedirectToAction(nameof(Index));
        }

        private Task<Cargo?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.Cargo.AsTracking()
                : _context.Cargo.AsNoTracking();

            return consulta.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        }

        private async Task ValidarRegrasAsync(CargoFormViewModel model, string nome, int empresaId, int? cargoId)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return;

            var nomeEmUso = await _context.Cargo
                .AsNoTracking()
                .AnyAsync(c => c.EmpresaId == empresaId
                            && c.Nome == nome
                            && (cargoId == null || c.Id != cargoId));

            if (nomeEmUso)
            {
                ModelState.AddModelError(nameof(model.Nome),
                    "Já existe um cargo com este nome nesta empresa.");
            }
        }

        private Task<int> ContarFuncionariosAsync(int cargoId, bool somenteAtivos)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = _context.Funcionarios
                .AsNoTracking()
                .Where(f => f.EmpresaId == empresaId && f.CargoId == cargoId);

            if (somenteAtivos)
                consulta = consulta.Where(f => f.Ativo);

            return consulta.CountAsync();
        }

        private async Task PreencherContagensAsync(CargoFormViewModel model)
        {
            model.QuantidadeFuncionarios = await ContarFuncionariosAsync(model.Id, somenteAtivos: false);
            model.QuantidadeFuncionariosAtivos = await ContarFuncionariosAsync(model.Id, somenteAtivos: true);
        }

        private async Task PreencherVinculosAsync(IReadOnlyList<CargoLinhaViewModel> cargos, int empresaId)
        {
            if (cargos.Count == 0)
                return;

            var ids = cargos.Select(c => c.Id).ToList();

            var funcionariosPorCargo = await _context.Funcionarios
                .AsNoTracking()
                .Where(f => f.EmpresaId == empresaId && ids.Contains(f.CargoId))
                .GroupBy(f => f.CargoId)
                .Select(grupo => new { CargoId = grupo.Key, Total = grupo.Count() })
                .ToDictionaryAsync(x => x.CargoId, x => x.Total);

            foreach (var cargo in cargos)
            {
                cargo.QuantidadeFuncionarios =
                    funcionariosPorCargo.TryGetValue(cargo.Id, out var total) ? total : 0;
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
                 WHERE coluna.object_id = OBJECT_ID('Tb_Cargo')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao, "Não foi possível prever o próximo id de Tb_Cargo.");
                return null;
            }
        }

        private static CargoFormViewModel ParaFormulario(Cargo cargo) => new()
        {
            Id = cargo.Id,
            Nome = cargo.Nome,
            Descricao = cargo.Descricao,
            SalarioBase = cargo.SalarioBase,
            PerComissaoBase = cargo.PerComissaoBase,
            Ativo = cargo.Ativo
        };

        private static string? TextoOuNulo(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        private static string FormatarSalario(decimal? valor) =>
            valor?.ToString("C") ?? "não definido";

        private static string FormatarComissao(decimal? valor) =>
            valor is decimal percentual ? $"{percentual:0.##}%" : "não definida";

        private static string NormalizarSituacao(string? situacao) => situacao switch
        {
            SituacaoFiltro.Ativos => SituacaoFiltro.Ativos,
            SituacaoFiltro.Inativos => SituacaoFiltro.Inativos,
            _ => SituacaoFiltro.Todos
        };
    }
}