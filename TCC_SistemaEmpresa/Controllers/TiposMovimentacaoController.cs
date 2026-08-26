using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE")]
    public class TiposMovimentacaoController : ControllerValidacao
    {
        private readonly ILogger<TiposMovimentacaoController> _logger;

        public TiposMovimentacaoController(AppDbContext context, ILogger<TiposMovimentacaoController> logger) : base(context)
        {
            _logger = logger;
        }

        protected override string EntidadeLog => nameof(TipoMovimentacao);

        [HttpGet]
        public async Task<IActionResult> Index(string? busca, string? situacao)
        {
            var empresaId = EmpresaIdAtual();
            situacao = NormalizarSituacao(situacao);

            var consulta = _context.TiposMovimentacao
                .AsNoTracking()
                .Where(t => t.EmpresaId == empresaId);

            consulta = situacao switch
            {
                SituacaoFiltro.Ativos => consulta.Where(t => t.Ativo),
                SituacaoFiltro.Inativos => consulta.Where(t => !t.Ativo),
                _ => consulta
            };

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(t => t.Nome.Contains(termo));
            }

            var tipos = await consulta
                .OrderBy(t => t.Nome)
                .Select(t => new TipoMovimentacaoLinhaViewModel
                {
                    Id = t.Id,
                    Nome = t.Nome,
                    Natureza = t.Natureza,
                    Ativo = t.Ativo
                })
                .ToListAsync();

            await PreencherVinculosAsync(tipos, empresaId);

            return View(new TipoMovimentacaoListaViewModel
            {
                Busca = busca,
                Situacao = situacao,
                Tipos = tipos
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var tipo = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (tipo is null)
                return NotFound();

            var model = ParaFormulario(tipo);
            model.SomenteLeitura = true;
            model.QuantidadeMovimentacoes = await ContarMovimentacoesAsync(id);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new TipoMovimentacaoFormViewModel
            {
                Ativo = true,
                ProximoId = await ProximoIdAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoMovimentacaoFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();

            ValidarNatureza(model.Natureza);

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                return View(model);
            }

            var tipo = new TipoMovimentacao
            {
                EmpresaId = empresaId,
                Nome = model.Nome.Trim(),
                Natureza = model.Natureza,
                Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim(),
                Ativo = model.Ativo
            };

            _context.TiposMovimentacao.Add(tipo);
            await _context.SaveChangesAsync();

            RegistrarLog("CRIACAO", tipo.Id,
                $"Tipo de movimentação '{tipo.Nome}' criado com natureza {tipo.Natureza}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Tipo de movimentação {TipoId} criado na empresa {EmpresaId} com natureza {Natureza}.",
                tipo.Id, empresaId, tipo.Natureza);

            TempData["Sucesso"] = $"Tipo de movimentação {tipo.Nome} cadastrado com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var tipo = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (tipo is null)
                return NotFound();

            var model = ParaFormulario(tipo);
            model.QuantidadeMovimentacoes = await ContarMovimentacoesAsync(id);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, TipoMovimentacaoFormViewModel model)
        {
            var tipo = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (tipo is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();
            var quantidadeMovimentacoes = await ContarMovimentacoesAsync(id);

            ValidarNatureza(model.Natureza);

            if (model.Natureza != tipo.Natureza && quantidadeMovimentacoes > 0)
            {
                ModelState.AddModelError(nameof(model.Natureza),
                    $"Este tipo já foi usado em {quantidadeMovimentacoes} movimentação(ões).");
            }

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.QuantidadeMovimentacoes = quantidadeMovimentacoes;
                return View(model);
            }

            var estavaAtivo = tipo.Ativo;

            tipo.Nome = model.Nome.Trim();
            tipo.Natureza = model.Natureza;
            tipo.Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim();
            tipo.Ativo = model.Ativo;

            var (acao, detalhe) = (estavaAtivo, model.Ativo) switch
            {
                (true, false) => ("INATIVACAO", "inativado"),
                (false, true) => ("REATIVACAO", "reativado"),
                _ => ("ALTERACAO", "alterado")
            };

            RegistrarLog(acao, tipo.Id, $"Tipo de movimentação '{tipo.Nome}' {detalhe}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Tipo de movimentação {TipoId} da empresa {EmpresaId}: {Acao}.",
                tipo.Id, empresaId, acao);

            TempData["Sucesso"] = $"Tipo de movimentação {tipo.Nome} {detalhe} com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromRoute] int id)
        {
            var tipo = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (tipo is null)
                return NotFound();

            var nome = tipo.Nome;

            if (tipo.Ativo)
            {
                TempData["Erro"] = $"O tipo {nome} precisa ser inativado antes de ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            var quantidadeMovimentacoes = await ContarMovimentacoesAsync(id);

            if (quantidadeMovimentacoes > 0)
            {
                TempData["Erro"] =
                    $"O tipo {nome} classifica {quantidadeMovimentacoes} movimentação(ões) de estoque e não pode ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            RegistrarLog("EXCLUSAO", tipo.Id,
                $"Tipo de movimentação '{nome}' excluído definitivamente.");

            _context.TiposMovimentacao.Remove(tipo);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Tipo de movimentação {TipoId} ({Nome}) excluído definitivamente por {Usuario}.",
                id, nome, User.Identity?.Name);

            TempData["Sucesso"] = $"Tipo de movimentação {nome} foi excluído definitivamente.";
            return RedirectToAction(nameof(Index));
        }

        private Task<TipoMovimentacao?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.TiposMovimentacao.AsTracking()
                : _context.TiposMovimentacao.AsNoTracking();

            return consulta.FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);
        }

        private void ValidarNatureza(string? natureza)
        {
            if (!string.IsNullOrWhiteSpace(natureza) && !NaturezaMovimentacao.EhValida(natureza))
                ModelState.AddModelError(nameof(TipoMovimentacaoFormViewModel.Natureza), "Tipo inválido.");
        }

        private Task<int> ContarMovimentacoesAsync(int tipoId)
        {
            var empresaId = EmpresaIdAtual();

            return _context.MovimentacoesEstoque
                .AsNoTracking()
                .CountAsync(m => m.TipoMovimentacaoEstoqueId == tipoId && m.EmpresaId == empresaId);
        }

        private async Task PreencherVinculosAsync(
            IReadOnlyList<TipoMovimentacaoLinhaViewModel> tipos, int empresaId)
        {
            if (tipos.Count == 0)
                return;

            var ids = tipos.Select(t => t.Id).ToList();

            var movimentacoesPorTipo = await _context.MovimentacoesEstoque
                .AsNoTracking()
                .Where(m => m.EmpresaId == empresaId && ids.Contains(m.TipoMovimentacaoEstoqueId))
                .GroupBy(m => m.TipoMovimentacaoEstoqueId)
                .Select(grupo => new { TipoId = grupo.Key, Total = grupo.Count() })
                .ToDictionaryAsync(x => x.TipoId, x => x.Total);

            foreach (var tipo in tipos)
            {
                tipo.QuantidadeMovimentacoes =
                    movimentacoesPorTipo.TryGetValue(tipo.Id, out var movimentacoes) ? movimentacoes : 0;
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
                 WHERE coluna.object_id = OBJECT_ID('Tb_Tipo_Movimentacao')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao, "Não foi possível prever o próximo id de Tb_Tipo_Movimentacao.");
                return null;
            }
        }

        private static TipoMovimentacaoFormViewModel ParaFormulario(TipoMovimentacao tipo) => new()
        {
            Id = tipo.Id,
            Nome = tipo.Nome,
            Natureza = tipo.Natureza,
            Descricao = tipo.Descricao,
            Ativo = tipo.Ativo
        };


        private static string NormalizarSituacao(string? situacao) => situacao switch
        {
            SituacaoFiltro.Ativos => SituacaoFiltro.Ativos,
            SituacaoFiltro.Inativos => SituacaoFiltro.Inativos,
            _ => SituacaoFiltro.Todos
        };
    }
}
