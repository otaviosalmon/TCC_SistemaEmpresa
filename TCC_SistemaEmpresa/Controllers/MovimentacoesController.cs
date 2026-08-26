using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE,ESTOQUISTA")]
    public class MovimentacoesController : ControllerValidacao
    {
        private readonly ILogger<MovimentacoesController> _logger;

        public MovimentacoesController(AppDbContext context, ILogger<MovimentacoesController> logger) :base(context)
        {
            _logger = logger;
        }

        protected override string EntidadeLog => nameof(MovimentacaoEstoque);

        [HttpGet]
        public async Task<IActionResult> Index(string? busca, string? natureza, int produtoId = 0)
        {
            var empresaId = EmpresaIdAtual();
            natureza = NormalizarNatureza(natureza);

            var consulta = _context.MovimentacoesEstoque
                .AsNoTracking()
                .Where(m => m.EmpresaId == empresaId);

            consulta = natureza switch
            {
                NaturezaFiltro.Entradas =>
                    consulta.Where(m => m.TipoMovimentacao.Natureza == NaturezaMovimentacao.Entrada),
                NaturezaFiltro.Saidas =>
                    consulta.Where(m => m.TipoMovimentacao.Natureza == NaturezaMovimentacao.Saida),
                _ => consulta
            };

            if (produtoId > 0)
                consulta = consulta.Where(m => m.ProdutoId == produtoId);

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(m =>
                    m.Produto.Nome.Contains(termo)
                    || m.TipoMovimentacao.Nome.Contains(termo)
                    || (m.Observacao != null && m.Observacao.Contains(termo)));
            }

            var movimentacoes = await consulta
                .OrderByDescending(m => m.DataMovimentacao)
                .ThenByDescending(m => m.Id)
                .Select(m => new MovimentacaoEstoqueLinhaViewModel
                {
                    Id = m.Id,
                    Produto = m.Produto.Nome,
                    Tipo = m.TipoMovimentacao.Nome,
                    Natureza = m.TipoMovimentacao.Natureza,
                    Quantidade = m.Quantidade,
                    QuantidadeAntes = m.QuantidadeAntes,
                    QuantidadeDepois = m.QuantidadeDepois,
                    DataMovimentacao = m.DataMovimentacao,
                    VendaId = m.VendaId
                })
                .ToListAsync();

            return View(new MovimentacaoEstoqueListaViewModel
            {
                Busca = busca,
                Natureza = natureza,
                ProdutoId = produtoId,
                Produtos = await CarregarFiltroProdutosAsync(produtoId),
                Movimentacoes = movimentacoes
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var movimentacao = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (movimentacao is null)
                return NotFound();

            var model = ParaFormulario(movimentacao);
            model.SomenteLeitura = true;
            model.Natureza = await NaturezaDoTipoAsync(movimentacao.TipoMovimentacaoEstoqueId);
            model.Produtos = await CarregarProdutosAsync(movimentacao.ProdutoId);
            model.Tipos = await CarregarTiposAsync(movimentacao.TipoMovimentacaoEstoqueId);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new MovimentacaoEstoqueFormViewModel
            {
                ProximoId = await ProximoIdAsync(),
                Produtos = await CarregarProdutosAsync(),
                Tipos = await CarregarTiposAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovimentacaoEstoqueFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();
            var usuarioId = UsuarioIdAtual();

            var tipo = await ValidarTipoAsync(model.TipoMovimentacaoEstoqueId, empresaId);

            if (usuarioId is null)
            {
                ModelState.AddModelError(string.Empty,
                    "Não foi possível identificar o usuário da sessão para registrar a movimentação. Refaça o login.");
            }

            await using var transacao = await _context.Database.BeginTransactionAsync();

            var produto = await ValidarProdutoAsync(model.ProdutoId, empresaId);

            var antes = produto?.QuantidadeAtual ?? 0;
            var depois = antes;

            if (produto is not null && tipo is not null && model.Quantidade > 0)
            {
                depois = antes + Efeito(tipo.Natureza, model.Quantidade.Value);

                if (depois < 0)
                {
                    ModelState.AddModelError(nameof(model.Quantidade),
                        $"Saldo insuficiente: o produto {produto.Nome} tem {antes} em estoque e a saída deixaria {depois}.");
                }
            }

            if (!ModelState.IsValid)
            {
                await transacao.RollbackAsync();
                return View(await ReconstruirFormularioAsync(model, tipo));
            }

            var movimentacao = new MovimentacaoEstoque
            {
                EmpresaId = empresaId,
                ProdutoId = model.ProdutoId,
                UsuarioId = usuarioId!.Value,
                TipoMovimentacaoEstoqueId = model.TipoMovimentacaoEstoqueId,
                Quantidade = model.Quantidade!.Value,
                QuantidadeAntes = antes,
                QuantidadeDepois = depois,
                DataMovimentacao = DateTime.Now,
                Observacao = string.IsNullOrWhiteSpace(model.Observacao) ? null : model.Observacao.Trim()
            };

            produto!.QuantidadeAtual = depois;
            _context.MovimentacoesEstoque.Add(movimentacao);
            await _context.SaveChangesAsync();

            RegistrarLog("CRIACAO", movimentacao.Id,
                $"{tipo!.Nome} de {movimentacao.Quantidade} un. do produto {produto.Nome}: saldo {antes} para {depois}.");
            await _context.SaveChangesAsync();
            await transacao.CommitAsync();

            _logger.LogInformation(
                "Movimentação {MovimentacaoId} registrada na empresa {EmpresaId}: produto {ProdutoId} de {Antes} para {Depois}.",
                movimentacao.Id, empresaId, produto.Id, antes, depois);

            TempData["Sucesso"] =
                $"Movimentação registrada: {produto.Nome} passou de {antes} para {depois} em estoque.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromRoute] int id)
        {
            var empresaId = EmpresaIdAtual();

            await using var transacao = await _context.Database.BeginTransactionAsync();

            var movimentacao = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (movimentacao is null)
                return NotFound();

            if (movimentacao.VendaId.HasValue)
            {
                TempData["Erro"] = MensagemOrigemVenda(id, movimentacao.VendaId.Value);
                return RedirectToAction(nameof(Index));
            }

            var tipo = await _context.TiposMovimentacao
                .AsNoTracking()
                .FirstAsync(t => t.Id == movimentacao.TipoMovimentacaoEstoqueId);

            var produto = await BloquearProdutoAsync(movimentacao.ProdutoId, empresaId);
            if (produto is null)
                return NotFound();

            var saldoAnterior = produto.QuantidadeAtual;
            var saldoRevertido = saldoAnterior - Efeito(tipo.Natureza, movimentacao.Quantidade);

            if (saldoRevertido < 0)
            {
                await transacao.RollbackAsync();

                TempData["Erro"] =
                    $"Excluir esta movimentação deixaria o produto {produto.Nome} com saldo {saldoRevertido}. " +
                    "Ajuste as movimentações posteriores antes de excluí-la.";
                return RedirectToAction(nameof(Index));
            }

            produto.QuantidadeAtual = saldoRevertido;

            RegistrarLog("EXCLUSAO", movimentacao.Id,
                $"{tipo.Nome} de {movimentacao.Quantidade} un. do produto {produto.Nome} excluída: " +
                $"saldo revertido de {saldoAnterior} para {saldoRevertido}.");

            _context.MovimentacoesEstoque.Remove(movimentacao);
            await _context.SaveChangesAsync();
            await transacao.CommitAsync();

            _logger.LogWarning(
                "Movimentação {MovimentacaoId} do produto {ProdutoId} excluída por {Usuario}: saldo de {Antes} para {Depois}.",
                id, produto.Id, User.Identity?.Name, saldoAnterior, saldoRevertido);

            TempData["Sucesso"] =
                $"Movimentação excluída: {produto.Nome} voltou de {saldoAnterior} para {saldoRevertido} em estoque.";
            return RedirectToAction(nameof(Index));
        }

        private Task<MovimentacaoEstoque?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.MovimentacoesEstoque.AsTracking()
                : _context.MovimentacoesEstoque.AsNoTracking();

            return consulta.FirstOrDefaultAsync(m => m.Id == id && m.EmpresaId == empresaId);
        }

        private Task<Produto?> BloquearProdutoAsync(int produtoId, int empresaId)
        {
            const string sql = @"
                SELECT *
                  FROM Tb_Produto WITH (UPDLOCK, ROWLOCK)
                 WHERE id = {0} AND empresa_id = {1}";

            return _context.Produtos
                .FromSqlRaw(sql, produtoId, empresaId)
                .AsTracking()
                .FirstOrDefaultAsync();
        }

        private async Task<Produto?> ValidarProdutoAsync(int produtoId, int empresaId)
        {
            if (produtoId <= 0)
                return null;

            var produto = await BloquearProdutoAsync(produtoId, empresaId);

            if (produto is null)
            {
                ModelState.AddModelError(nameof(MovimentacaoEstoqueFormViewModel.ProdutoId), "Produto inválido.");
                return null;
            }

            if (!produto.Ativo)
            {
                ModelState.AddModelError(nameof(MovimentacaoEstoqueFormViewModel.ProdutoId),
                    $"O produto {produto.Nome} está inativo e não aceita novas movimentações.");
                return null;
            }

            return produto;
        }

        private async Task<TipoMovimentacao?> ValidarTipoAsync(int tipoId, int empresaId)
        {
            if (tipoId <= 0)
                return null;

            var tipo = await _context.TiposMovimentacao
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tipoId && t.EmpresaId == empresaId);

            if (tipo is null || !NaturezaMovimentacao.EhValida(tipo.Natureza))
            {
                ModelState.AddModelError(nameof(MovimentacaoEstoqueFormViewModel.TipoMovimentacaoEstoqueId),
                    "Tipo de movimentação inválido.");
                return null;
            }

            if (!tipo.Ativo)
            {
                ModelState.AddModelError(nameof(MovimentacaoEstoqueFormViewModel.TipoMovimentacaoEstoqueId),
                    $"O tipo {tipo.Nome} está inativo e não aceita novas movimentações.");
                return null;
            }

            return tipo;
        }

        private async Task<MovimentacaoEstoqueFormViewModel> ReconstruirFormularioAsync(
            MovimentacaoEstoqueFormViewModel model, TipoMovimentacao? tipo)
        {
            model.Natureza = tipo?.Natureza;
            model.ProximoId = await ProximoIdAsync();
            model.Produtos = await CarregarProdutosAsync(model.ProdutoId);
            model.Tipos = await CarregarTiposAsync(model.TipoMovimentacaoEstoqueId);
            return model;
        }

        private async Task<IReadOnlyList<ProdutoOpcaoViewModel>> CarregarProdutosAsync(int? selecionado = null)
        {
            var empresaId = EmpresaIdAtual();

            return await _context.Produtos
                .AsNoTracking()
                .Where(p => p.EmpresaId == empresaId
                    && (p.Ativo || (selecionado.HasValue && p.Id == selecionado.Value)))
                .OrderBy(p => p.Nome)
                .Select(p => new ProdutoOpcaoViewModel
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Saldo = p.QuantidadeAtual,
                    Ativo = p.Ativo
                })
                .ToListAsync();
        }

        private async Task<IReadOnlyList<TipoMovimentacaoOpcaoViewModel>> CarregarTiposAsync(int? selecionado = null)
        {
            var empresaId = EmpresaIdAtual();

            return await _context.TiposMovimentacao
                .AsNoTracking()
                .Where(t => t.EmpresaId == empresaId
                    && (t.Ativo || (selecionado.HasValue && t.Id == selecionado.Value)))
                .OrderBy(t => t.Nome)
                .Select(t => new TipoMovimentacaoOpcaoViewModel
                {
                    Id = t.Id,
                    Nome = t.Nome,
                    Natureza = t.Natureza,
                    Ativo = t.Ativo
                })
                .ToListAsync();
        }

        private async Task<IEnumerable<SelectListItem>> CarregarFiltroProdutosAsync(int selecionado)
        {
            var empresaId = EmpresaIdAtual();

            var produtos = await _context.Produtos
                .AsNoTracking()
                .Where(p => p.EmpresaId == empresaId)
                .OrderBy(p => p.Nome)
                .Select(p => new { p.Id, p.Nome })
                .ToListAsync();

            return produtos.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Nome,
                Selected = p.Id == selecionado
            });
        }

        private Task<string?> NaturezaDoTipoAsync(int tipoId)
        {
            return _context.TiposMovimentacao
                .AsNoTracking()
                .Where(t => t.Id == tipoId)
                .Select(t => t.Natureza)
                .FirstOrDefaultAsync();
        }

        private async Task<int?> ProximoIdAsync()
        {
            const string sql = @"
                SELECT CASE
                           WHEN coluna.last_value IS NULL THEN CONVERT(int, coluna.seed_value)
                           ELSE CONVERT(int, coluna.last_value) + CONVERT(int, coluna.increment_value)
                       END AS Value
                  FROM sys.identity_columns AS coluna
                 WHERE coluna.object_id = OBJECT_ID('Tb_Movimentacao_Estoque')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao, "Não foi possível prever o próximo id de Tb_Movimentacao_Estoque.");
                return null;
            }
        }

        private static MovimentacaoEstoqueFormViewModel ParaFormulario(MovimentacaoEstoque movimentacao) => new()
        {
            Id = movimentacao.Id,
            ProdutoId = movimentacao.ProdutoId,
            TipoMovimentacaoEstoqueId = movimentacao.TipoMovimentacaoEstoqueId,
            DataMovimentacao = movimentacao.DataMovimentacao,
            Quantidade = movimentacao.Quantidade,
            QuantidadeAntes = movimentacao.QuantidadeAntes,
            QuantidadeDepois = movimentacao.QuantidadeDepois,
            Observacao = movimentacao.Observacao,
            VendaId = movimentacao.VendaId
        };

        private static string MensagemOrigemVenda(int movimentacaoId, int vendaId) =>
            $"A movimentação {movimentacaoId} foi gerada pela venda {vendaId} e só pode ser desfeita pelo cancelamento da venda.";

        private static int Efeito(string? natureza, int quantidade) =>
            natureza == NaturezaMovimentacao.Entrada ? quantidade : -quantidade;


        private static string NormalizarNatureza(string? natureza) => natureza switch
        {
            NaturezaFiltro.Entradas => NaturezaFiltro.Entradas,
            NaturezaFiltro.Saidas => NaturezaFiltro.Saidas,
            _ => NaturezaFiltro.Todas
        };
    }
}
