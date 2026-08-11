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
    [Authorize(Roles = "ADMIN,GERENTE")]
    public class ProdutosController : Controller
    {
        private const string NaturezaEntrada = "ENTRADA";

        private readonly AppDbContext _context;
        private readonly ILogger<ProdutosController> _logger;

        public ProdutosController(AppDbContext context, ILogger<ProdutosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? busca, string? situacao)
        {
            var empresaId = EmpresaIdAtual();
            situacao = NormalizarSituacao(situacao);

            var consulta = _context.Produtos
                .AsNoTracking()
                .Where(p => p.EmpresaId == empresaId);

            consulta = situacao switch
            {
                SituacaoFiltro.Ativos => consulta.Where(p => p.Ativo),
                SituacaoFiltro.Inativos => consulta.Where(p => !p.Ativo),
                _ => consulta
            };

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(p => p.Nome.Contains(termo));
            }

            var produtos = await consulta
                .OrderBy(p => p.Nome)
                .Select(p => new ProdutoLinhaViewModel
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Categoria = p.CategoriaProduto.Nome,
                    PrecoVenda = p.PrecoVenda,
                    QuantidadeAtual = p.QuantidadeAtual,
                    EstoqueMinimo = p.EstoqueMinimo,
                    Ativo = p.Ativo
                })
                .ToListAsync();

            await PreencherVinculosAsync(produtos, empresaId);

            return View(new ProdutoListaViewModel
            {
                Busca = busca,
                Situacao = situacao,
                Produtos = produtos
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var produto = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (produto is null)
                return NotFound();

            var model = ParaFormulario(produto);
            model.SomenteLeitura = true;
            model.Categorias = await CarregarCategoriasAsync(produto.CategoriaProdutoId);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ProdutoFormViewModel
            {
                Ativo = true,
                QuantidadeAtual = 0,
                ProximoId = await ProximoIdAsync(),
                Categorias = await CarregarCategoriasAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProdutoFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();
            var usuarioId = UsuarioIdAtual();
            var estoqueInicial = model.QuantidadeAtual ?? 0;

            await ValidarRegrasAsync(model, empresaId);

            var tipoEntrada = estoqueInicial > 0
                ? await BuscarTipoEntradaAsync(empresaId)
                : null;

            if (estoqueInicial > 0 && tipoEntrada is null)
            {
                ModelState.AddModelError(nameof(model.QuantidadeAtual),
                    "Nenhum tipo de movimentação com natureza ENTRADA está cadastrado. " +
                    "Cadastre um antes de informar estoque inicial, ou deixe o campo em zero.");
            }

            if (estoqueInicial > 0 && usuarioId is null)
            {
                ModelState.AddModelError(nameof(model.QuantidadeAtual),
                    "Não foi possível identificar o usuário da sessão para registrar a entrada " +
                    "de estoque. Refaça o login, ou deixe o estoque inicial em zero.");
            }

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                model.Categorias = await CarregarCategoriasAsync(model.CategoriaProdutoId);
                return View(model);
            }

            var produto = new Produto
            {
                EmpresaId = empresaId,
                CategoriaProdutoId = model.CategoriaProdutoId,
                Nome = model.Nome.Trim(),
                Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim(),
                PrecoCusto = model.PrecoCusto!.Value,
                PrecoVenda = model.PrecoVenda!.Value,
                QuantidadeAtual = estoqueInicial,
                EstoqueMinimo = model.EstoqueMinimo,
                DataCadastro = DateTime.Now,
                Ativo = model.Ativo
            };

            await using var transacao = await _context.Database.BeginTransactionAsync();

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            if (estoqueInicial > 0)
            {
                _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    EmpresaId = empresaId,
                    ProdutoId = produto.Id,
                    UsuarioId = usuarioId!.Value,
                    TipoMovimentacaoEstoqueId = tipoEntrada!.Id,
                    Quantidade = estoqueInicial,
                    QuantidadeAntes = 0,
                    QuantidadeDepois = estoqueInicial,
                    DataMovimentacao = DateTime.Now,
                    Observacao = "Estoque inicial informado no cadastro do produto."
                });
            }

            RegistrarLog("CRIACAO", produto.Id,
                $"Produto '{produto.Nome}' criado (categoria {produto.CategoriaProdutoId}, " +
                $"estoque inicial {estoqueInicial}).");

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();

            _logger.LogInformation(
                "Produto {ProdutoId} criado na empresa {EmpresaId} com estoque inicial {Estoque}.",
                produto.Id, empresaId, estoqueInicial);

            TempData["Sucesso"] = $"Produto {produto.Nome} cadastrado com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var produto = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (produto is null)
                return NotFound();

            var model = ParaFormulario(produto);
            model.Categorias = await CarregarCategoriasAsync(produto.CategoriaProdutoId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, ProdutoFormViewModel model)
        {
            var produto = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (produto is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();

            await ValidarRegrasAsync(model, empresaId);

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.DataCadastro = produto.DataCadastro;
                model.QuantidadeAtual = produto.QuantidadeAtual;
                model.Categorias = await CarregarCategoriasAsync(model.CategoriaProdutoId);
                return View(model);
            }

            var estavaAtivo = produto.Ativo;

            produto.CategoriaProdutoId = model.CategoriaProdutoId;
            produto.Nome = model.Nome.Trim();
            produto.Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim();
            produto.PrecoCusto = model.PrecoCusto!.Value;
            produto.PrecoVenda = model.PrecoVenda!.Value;
            produto.EstoqueMinimo = model.EstoqueMinimo;
            produto.Ativo = model.Ativo;

            var (acao, detalhe) = (estavaAtivo, model.Ativo) switch
            {
                (true, false) => ("INATIVACAO", "inativado"),
                (false, true) => ("REATIVACAO", "reativado"),
                _ => ("ALTERACAO", "alterado")
            };

            RegistrarLog(acao, produto.Id, $"Produto '{produto.Nome}' {detalhe}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Produto {ProdutoId} da empresa {EmpresaId}: {Acao}.",
                produto.Id, empresaId, acao);

            TempData["Sucesso"] = $"Produto {produto.Nome} {detalhe} com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromRoute] int id)
        {
            var produto = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (produto is null)
                return NotFound();

            var nome = produto.Nome;
            var empresaId = EmpresaIdAtual();

            if (produto.Ativo)
            {
                TempData["Erro"] = $"O produto {nome} precisa ser inativado antes de ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            var quantidadeItens = await _context.ItensVenda
                .AsNoTracking()
                .CountAsync(i => i.ProdutoId == id && i.Venda.EmpresaId == empresaId);

            if (quantidadeItens > 0)
            {
                TempData["Erro"] =
                    $"O produto {nome} aparece em {quantidadeItens} item(ns) de venda e não pode ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            var quantidadeMovimentacoes = await _context.MovimentacoesEstoque
                .AsNoTracking()
                .CountAsync(m => m.ProdutoId == id && m.EmpresaId == empresaId);

            if (quantidadeMovimentacoes > 0)
            {
                TempData["Erro"] =
                    $"O produto {nome} possui {quantidadeMovimentacoes} movimentação(ões) de estoque e não pode ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            RegistrarLog("EXCLUSAO", produto.Id,
                $"Produto '{nome}' excluído definitivamente.");

            _context.Produtos.Remove(produto);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Produto {ProdutoId} ({Nome}) excluído definitivamente por {Usuario}.",
                id, nome, User.Identity?.Name);

            TempData["Sucesso"] = $"Produto {nome} foi excluído definitivamente.";
            return RedirectToAction(nameof(Index));
        }

        private Task<Produto?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.Produtos.AsTracking()
                : _context.Produtos.AsNoTracking();

            return consulta.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        }

        private async Task ValidarRegrasAsync(ProdutoFormViewModel model, int empresaId)
        {
            if (model.CategoriaProdutoId > 0)
            {
                var categoriaValida = await _context.CategoriaProdutos
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == model.CategoriaProdutoId && c.EmpresaId == empresaId);

                if (!categoriaValida)
                    ModelState.AddModelError(nameof(model.CategoriaProdutoId), "Tipo de produto inválido.");
            }

            if (model.PrecoCusto.HasValue
                && model.PrecoVenda.HasValue
                && model.PrecoVenda.Value < model.PrecoCusto.Value)
            {
                ModelState.AddModelError(nameof(model.PrecoVenda),
                    "O preço de venda não pode ser menor que o preço de custo.");
            }
        }

        private async Task PreencherVinculosAsync(
            IReadOnlyList<ProdutoLinhaViewModel> produtos, int empresaId)
        {
            if (produtos.Count == 0)
                return;

            var ids = produtos.Select(p => p.Id).ToList();

            var itensPorProduto = await _context.ItensVenda
                .AsNoTracking()
                .Where(i => ids.Contains(i.ProdutoId) && i.Venda.EmpresaId == empresaId)
                .GroupBy(i => i.ProdutoId)
                .Select(grupo => new { ProdutoId = grupo.Key, Total = grupo.Count() })
                .ToDictionaryAsync(x => x.ProdutoId, x => x.Total);

            var movimentacoesPorProduto = await _context.MovimentacoesEstoque
                .AsNoTracking()
                .Where(m => m.EmpresaId == empresaId && ids.Contains(m.ProdutoId))
                .GroupBy(m => m.ProdutoId)
                .Select(grupo => new { ProdutoId = grupo.Key, Total = grupo.Count() })
                .ToDictionaryAsync(x => x.ProdutoId, x => x.Total);

            foreach (var produto in produtos)
            {
                produto.QuantidadeItensVenda =
                    itensPorProduto.TryGetValue(produto.Id, out var itens) ? itens : 0;

                produto.QuantidadeMovimentacoes =
                    movimentacoesPorProduto.TryGetValue(produto.Id, out var movimentacoes) ? movimentacoes : 0;
            }
        }

        private Task<TipoMovimentacao?> BuscarTipoEntradaAsync(int empresaId)
        {
            return _context.TiposMovimentacao
                .AsNoTracking()
                .Where(t => t.EmpresaId == empresaId && t.Ativo && t.Natureza == NaturezaEntrada)
                .OrderBy(t => t.Id)
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
                 WHERE coluna.object_id = OBJECT_ID('Tb_Produto')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao, "Não foi possível prever o próximo id de Tb_Produto.");
                return null;
            }
        }

        private async Task<IEnumerable<SelectListItem>> CarregarCategoriasAsync(int? selecionado = null)
        {
            var empresaId = EmpresaIdAtual();

            var categorias = await _context.CategoriaProdutos
                .AsNoTracking()
                .Where(c => c.EmpresaId == empresaId)
                .OrderBy(c => c.Nome)
                .Select(c => new { c.Id, c.Nome })
                .ToListAsync();

            return categorias.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Nome,
                Selected = selecionado.HasValue && c.Id == selecionado.Value
            });
        }

        private static ProdutoFormViewModel ParaFormulario(Produto produto) => new()
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Ativo = produto.Ativo,
            PrecoCusto = produto.PrecoCusto,
            PrecoVenda = produto.PrecoVenda,
            QuantidadeAtual = produto.QuantidadeAtual,
            EstoqueMinimo = produto.EstoqueMinimo,
            DataCadastro = produto.DataCadastro,
            CategoriaProdutoId = produto.CategoriaProdutoId
        };

        private void RegistrarLog(string acao, int registroId, string detalhes)
        {
            _context.LogsSistema.Add(new LogSistema
            {
                EmpresaId = EmpresaIdAtual(),
                UsuarioId = UsuarioIdAtual(),
                Acao = acao,
                EntidadeAfetada = nameof(Produto),
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
