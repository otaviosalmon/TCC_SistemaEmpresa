using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;
using TCC_SistemaEmpresa.Validation;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE")]
    public class ClientesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(AppDbContext context, ILogger<ClientesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? busca, string? situacao)
        {
            var empresaId = EmpresaIdAtual();
            situacao = NormalizarSituacao(situacao);

            var consulta = _context.Clientes
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
                var digitos = CpfAttribute.ApenasDigitos(termo);

                consulta = digitos.Length > 0
                    ? consulta.Where(c => c.Nome.Contains(termo)
                                       || c.Email!.Contains(termo)
                                       || c.Telefone!.Contains(termo)
                                       || c.Cpf!.Contains(digitos))
                    : consulta.Where(c => c.Nome.Contains(termo)
                                       || c.Email!.Contains(termo));
            }

            var clientes = await consulta
                .OrderBy(c => c.Nome)
                .Select(c => new ClienteLinhaViewModel
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Telefone = c.Telefone,
                    Email = c.Email,
                    Ativo = c.Ativo
                })
                .ToListAsync();

            await PreencherVinculosAsync(clientes, empresaId);

            return View(new ClienteListaViewModel
            {
                Busca = busca,
                Situacao = situacao,
                Clientes = clientes
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var cliente = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (cliente is null)
                return NotFound();

            var model = ParaFormulario(cliente);
            model.SomenteLeitura = true;
            model.QuantidadeVendas = await ContarVendasAsync(id);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ClienteFormViewModel
            {
                Ativo = true,
                ProximoId = await ProximoIdAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClienteFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();
            var cpf = NormalizarCpf(model.Cpf);
            var email = NormalizarTexto(model.Email);

            await ValidarRegrasAsync(model, cpf, email, empresaId, clienteId: null);

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                return View(model);
            }

            var cliente = new Cliente
            {
                EmpresaId = empresaId,
                Nome = model.Nome.Trim(),
                Cpf = cpf,
                Email = email,
                Telefone = NormalizarTexto(model.Telefone),
                Endereco = NormalizarTexto(model.Endereco),
                DataCadastro = DateTime.Now,
                Ativo = model.Ativo
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            RegistrarLog("CRIACAO", cliente.Id, $"Cliente '{cliente.Nome}' criado.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cliente {ClienteId} criado na empresa {EmpresaId}.",
                cliente.Id, empresaId);

            TempData["Sucesso"] = $"Cliente {cliente.Nome} cadastrado com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var cliente = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (cliente is null)
                return NotFound();

            var model = ParaFormulario(cliente);
            model.QuantidadeVendas = await ContarVendasAsync(id);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, ClienteFormViewModel model)
        {
            var cliente = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (cliente is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();
            var cpf = NormalizarCpf(model.Cpf);
            var email = NormalizarTexto(model.Email);

            await ValidarRegrasAsync(model, cpf, email, empresaId, clienteId: id);

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.DataCadastro = cliente.DataCadastro;
                model.QuantidadeVendas = await ContarVendasAsync(id);
                return View(model);
            }

            var estavaAtivo = cliente.Ativo;

            cliente.Nome = model.Nome.Trim();
            cliente.Cpf = cpf;
            cliente.Email = email;
            cliente.Telefone = NormalizarTexto(model.Telefone);
            cliente.Endereco = NormalizarTexto(model.Endereco);
            cliente.Ativo = model.Ativo;

            var (acao, detalhe) = (estavaAtivo, model.Ativo) switch
            {
                (true, false) => ("INATIVACAO", "inativado"),
                (false, true) => ("REATIVACAO", "reativado"),
                _ => ("ALTERACAO", "alterado")
            };

            RegistrarLog(acao, cliente.Id, $"Cliente '{cliente.Nome}' {detalhe}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cliente {ClienteId} da empresa {EmpresaId}: {Acao}.",
                cliente.Id, empresaId, acao);

            TempData["Sucesso"] = $"Cliente {cliente.Nome} {detalhe} com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromRoute] int id)
        {
            var cliente = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (cliente is null)
                return NotFound();

            var nome = cliente.Nome;

            if (cliente.Ativo)
            {
                TempData["Erro"] = $"O cliente {nome} precisa ser inativado antes de ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            var quantidadeVendas = await ContarVendasAsync(id);

            if (quantidadeVendas > 0)
            {
                TempData["Erro"] =
                    $"O cliente {nome} possui {quantidadeVendas} venda(s) vinculada(s) e não pode ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            RegistrarLog("EXCLUSAO", cliente.Id, $"Cliente '{nome}' excluído definitivamente.");

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Cliente {ClienteId} ({Nome}) excluído definitivamente por {Usuario}.",
                id, nome, User.Identity?.Name);

            TempData["Sucesso"] = $"Cliente {nome} foi excluído definitivamente.";
            return RedirectToAction(nameof(Index));
        }

        private Task<Cliente?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.Clientes.AsTracking()
                : _context.Clientes.AsNoTracking();

            return consulta.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        }

        private async Task ValidarRegrasAsync(
            ClienteFormViewModel model, string? cpf, string? email, int empresaId, int? clienteId)
        {
            if (cpf is not null)
            {
                var cpfEmUso = await _context.Clientes
                    .AsNoTracking()
                    .AnyAsync(c => c.EmpresaId == empresaId
                                && c.Cpf == cpf
                                && (clienteId == null || c.Id != clienteId));

                if (cpfEmUso)
                    ModelState.AddModelError(nameof(model.Cpf),
                        "Já existe um cliente com este CPF nesta empresa.");
            }

            if (email is not null)
            {
                var emailEmUso = await _context.Clientes
                    .AsNoTracking()
                    .AnyAsync(c => c.EmpresaId == empresaId
                                && c.Email == email
                                && (clienteId == null || c.Id != clienteId));

                if (emailEmUso)
                    ModelState.AddModelError(nameof(model.Email),
                        "Já existe um cliente com este e-mail nesta empresa.");
            }
        }

        private Task<int> ContarVendasAsync(int clienteId)
        {
            var empresaId = EmpresaIdAtual();

            return _context.Vendas
                .AsNoTracking()
                .CountAsync(v => v.ClienteId == clienteId && v.EmpresaId == empresaId);
        }

        private async Task PreencherVinculosAsync(
            IReadOnlyList<ClienteLinhaViewModel> clientes, int empresaId)
        {
            if (clientes.Count == 0)
                return;

            var ids = clientes.Select(c => c.Id).ToList();

            var vendasPorCliente = await _context.Vendas
                .AsNoTracking()
                .Where(v => v.EmpresaId == empresaId
                         && v.ClienteId != null
                         && ids.Contains(v.ClienteId.Value))
                .GroupBy(v => v.ClienteId!.Value)
                .Select(grupo => new { ClienteId = grupo.Key, Total = grupo.Count() })
                .ToDictionaryAsync(x => x.ClienteId, x => x.Total);

            foreach (var cliente in clientes)
            {
                cliente.QuantidadeVendas =
                    vendasPorCliente.TryGetValue(cliente.Id, out var vendas) ? vendas : 0;
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
                 WHERE coluna.object_id = OBJECT_ID('Tb_Cliente')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao, "Não foi possível prever o próximo id de Tb_Cliente.");
                return null;
            }
        }

        private static ClienteFormViewModel ParaFormulario(Cliente cliente) => new()
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            Cpf = cliente.Cpf,
            Email = cliente.Email,
            Telefone = cliente.Telefone,
            Endereco = cliente.Endereco,
            DataCadastro = cliente.DataCadastro,
            Ativo = cliente.Ativo
        };

        private static string? NormalizarCpf(string? cpf)
        {
            var digitos = CpfAttribute.ApenasDigitos(cpf);
            return digitos.Length == 0 ? null : digitos;
        }

        private static string? NormalizarTexto(string? texto)
            => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        private void RegistrarLog(string acao, int registroId, string detalhes)
        {
            _context.LogsSistema.Add(new LogSistema
            {
                EmpresaId = EmpresaIdAtual(),
                UsuarioId = UsuarioIdAtual(),
                Acao = acao,
                EntidadeAfetada = nameof(Cliente),
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
