using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE")]
    public class FormasPagamentoController : ControllerValidacao
    {
        private readonly ILogger<FormasPagamentoController> _logger;

        public FormasPagamentoController(AppDbContext context, ILogger<FormasPagamentoController> logger) : base(context)
        {
            _logger = logger;
        }

        protected override string EntidadeLog => nameof(FormaPagamento);

        [HttpGet]
        public async Task<IActionResult> Index(string? busca, string? situacao)
        {
            var empresaId = EmpresaIdAtual();
            situacao = NormalizarSituacao(situacao);

            var consulta = _context.FormasPagamento
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
                consulta = consulta.Where(f => f.Nome.Contains(termo));
            }

            var formas = await consulta
                .OrderBy(f => f.Nome)
                .Select(f => new FormaPagamentoLinhaViewModel
                {
                    Id = f.Id,
                    Nome = f.Nome,
                    Ativo = f.Ativo
                })
                .ToListAsync();

            await PreencherVinculosAsync(formas, empresaId);

            return View(new FormaPagamentoListaViewModel
            {
                Busca = busca,
                Situacao = situacao,
                Formas = formas
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var forma = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (forma is null)
                return NotFound();

            var model = ParaFormulario(forma);
            model.SomenteLeitura = true;
            model.QuantidadeVendas = await ContarVendasAsync(id);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new FormaPagamentoFormViewModel
            {
                Ativo = true,
                ProximoId = await ProximoIdAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FormaPagamentoFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                return View(model);
            }

            var forma = new FormaPagamento
            {
                EmpresaId = empresaId,
                Nome = model.Nome.Trim(),
                Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim(),
                Ativo = model.Ativo
            };

            _context.FormasPagamento.Add(forma);
            await _context.SaveChangesAsync();

            RegistrarLog("CRIACAO", forma.Id, $"Forma de pagamento '{forma.Nome}' criada.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Forma de pagamento {FormaId} criada na empresa {EmpresaId}.",
                forma.Id, empresaId);

            TempData["Sucesso"] = $"Forma de pagamento {forma.Nome} cadastrada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var forma = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (forma is null)
                return NotFound();

            var model = ParaFormulario(forma);
            model.QuantidadeVendas = await ContarVendasAsync(id);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, FormaPagamentoFormViewModel model)
        {
            var forma = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (forma is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.QuantidadeVendas = await ContarVendasAsync(id);
                return View(model);
            }

            var estavaAtiva = forma.Ativo;

            forma.Nome = model.Nome.Trim();
            forma.Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim();
            forma.Ativo = model.Ativo;

            var (acao, detalhe) = (estavaAtiva, model.Ativo) switch
            {
                (true, false) => ("INATIVACAO", "inativada"),
                (false, true) => ("REATIVACAO", "reativada"),
                _ => ("ALTERACAO", "alterada")
            };

            RegistrarLog(acao, forma.Id, $"Forma de pagamento '{forma.Nome}' {detalhe}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Forma de pagamento {FormaId} da empresa {EmpresaId}: {Acao}.",
                forma.Id, empresaId, acao);

            TempData["Sucesso"] = $"Forma de pagamento {forma.Nome} {detalhe} com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromRoute] int id)
        {
            var forma = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (forma is null)
                return NotFound();

            var nome = forma.Nome;

            if (forma.Ativo)
            {
                TempData["Erro"] = $"A forma de pagamento {nome} precisa ser inativada antes de ser excluída.";
                return RedirectToAction(nameof(Index));
            }

            var quantidadeVendas = await ContarVendasAsync(id);

            if (quantidadeVendas > 0)
            {
                TempData["Erro"] =
                    $"A forma de pagamento {nome} está registrada em {quantidadeVendas} venda(s) e não pode ser excluída.";
                return RedirectToAction(nameof(Index));
            }

            RegistrarLog("EXCLUSAO", forma.Id, $"Forma de pagamento '{nome}' excluída definitivamente.");

            _context.FormasPagamento.Remove(forma);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Forma de pagamento {FormaId} ({Nome}) excluída definitivamente por {Usuario}.",
                id, nome, User.Identity?.Name);

            TempData["Sucesso"] = $"Forma de pagamento {nome} foi excluída definitivamente.";
            return RedirectToAction(nameof(Index));
        }

        private Task<FormaPagamento?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.FormasPagamento.AsTracking()
                : _context.FormasPagamento.AsNoTracking();

            return consulta.FirstOrDefaultAsync(f => f.Id == id && f.EmpresaId == empresaId);
        }

        private Task<int> ContarVendasAsync(int formaPagamentoId)
        {
            var empresaId = EmpresaIdAtual();

            return _context.Vendas
                .AsNoTracking()
                .CountAsync(v => v.FormaPagamentoId == formaPagamentoId && v.EmpresaId == empresaId);
        }

        private async Task PreencherVinculosAsync(
            IReadOnlyList<FormaPagamentoLinhaViewModel> formas, int empresaId)
        {
            if (formas.Count == 0)
                return;

            var ids = formas.Select(f => f.Id).ToList();

            var vendasPorForma = await _context.Vendas
                .AsNoTracking()
                .Where(v => v.EmpresaId == empresaId && ids.Contains(v.FormaPagamentoId))
                .GroupBy(v => v.FormaPagamentoId)
                .Select(grupo => new { FormaPagamentoId = grupo.Key, Total = grupo.Count() })
                .ToDictionaryAsync(x => x.FormaPagamentoId, x => x.Total);

            foreach (var forma in formas)
            {
                forma.QuantidadeVendas =
                    vendasPorForma.TryGetValue(forma.Id, out var vendas) ? vendas : 0;
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
                 WHERE coluna.object_id = OBJECT_ID('Tb_Forma_Pagamento')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao, "Não foi possível prever o próximo id de Tb_Forma_Pagamento.");
                return null;
            }
        }

        private static FormaPagamentoFormViewModel ParaFormulario(FormaPagamento forma) => new()
        {
            Id = forma.Id,
            Nome = forma.Nome,
            Descricao = forma.Descricao,
            Ativo = forma.Ativo
        };

        private static string NormalizarSituacao(string? situacao) => situacao switch
        {
            SituacaoFiltro.Ativos => SituacaoFiltro.Ativos,
            SituacaoFiltro.Inativos => SituacaoFiltro.Inativos,
            _ => SituacaoFiltro.Todos
        };
    }
}
