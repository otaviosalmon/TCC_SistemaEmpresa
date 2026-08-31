using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE")]
    public class CategoriasController : ControllerValidacao
    {
        private readonly ILogger<CategoriasController> _logger;

        public CategoriasController(AppDbContext context, ILogger<CategoriasController> logger) : base(context)
        {
            _logger = logger;
        }

        protected override string EntidadeLog => nameof(CategoriaProduto);

        [HttpGet]
        public async Task<IActionResult> Index(string? busca)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = _context.CategoriaProdutos
                .AsNoTracking()
                .Where(c => c.EmpresaId == empresaId);

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(c => c.Nome.Contains(termo));
            }

            var categorias = await consulta
                .OrderBy(c => c.Nome)
                .Select(c => new CategoriaProdutoLinhaViewModel
                {
                    Id = c.Id,
                    Nome = c.Nome
                })
                .ToListAsync();

            await PreencherVinculosAsync(categorias, empresaId);

            return View(new CategoriaProdutoListaViewModel
            {
                Busca = busca,
                Categorias = categorias
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var categoria = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (categoria is null)
                return NotFound();

            var model = ParaFormulario(categoria);
            model.SomenteLeitura = true;
            model.QuantidadeProdutos = await ContarProdutosAsync(id);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CategoriaProdutoFormViewModel
            {
                ProximoId = await ProximoIdAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoriaProdutoFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                return View(model);
            }

            var categoria = new CategoriaProduto
            {
                EmpresaId = empresaId,
                Nome = model.Nome.Trim(),
                Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim()
            };

            _context.CategoriaProdutos.Add(categoria);
            await _context.SaveChangesAsync();

            RegistrarLog("CRIACAO", categoria.Id, $"Tipo de produto '{categoria.Nome}' criado.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Tipo de produto {CategoriaId} criado na empresa {EmpresaId}.",
                categoria.Id, empresaId);

            TempData["Sucesso"] = $"Tipo de produto {categoria.Nome} cadastrado com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var categoria = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (categoria is null)
                return NotFound();

            var model = ParaFormulario(categoria);
            model.QuantidadeProdutos = await ContarProdutosAsync(id);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, CategoriaProdutoFormViewModel model)
        {
            var categoria = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (categoria is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.QuantidadeProdutos = await ContarProdutosAsync(id);
                return View(model);
            }

            categoria.Nome = model.Nome.Trim();
            categoria.Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim();

            RegistrarLog("ALTERACAO", categoria.Id, $"Tipo de produto '{categoria.Nome}' alterado.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Tipo de produto {CategoriaId} da empresa {EmpresaId} alterado.",
                categoria.Id, empresaId);

            TempData["Sucesso"] = $"Tipo de produto {categoria.Nome} alterado com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromRoute] int id)
        {
            var categoria = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (categoria is null)
                return NotFound();

            var nome = categoria.Nome;
            var quantidadeProdutos = await ContarProdutosAsync(id);

            if (quantidadeProdutos > 0)
            {
                TempData["Erro"] =
                    $"O tipo {nome} classifica {quantidadeProdutos} produto(s) e não pode ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            RegistrarLog("EXCLUSAO", categoria.Id, $"Tipo de produto '{nome}' excluído definitivamente.");

            _context.CategoriaProdutos.Remove(categoria);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Tipo de produto {CategoriaId} ({Nome}) excluído definitivamente por {Usuario}.",
                id, nome, User.Identity?.Name);

            TempData["Sucesso"] = $"Tipo de produto {nome} foi excluído definitivamente.";
            return RedirectToAction(nameof(Index));
        }

        private Task<CategoriaProduto?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.CategoriaProdutos.AsTracking()
                : _context.CategoriaProdutos.AsNoTracking();

            return consulta.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        }

        private Task<int> ContarProdutosAsync(int categoriaId)
        {
            var empresaId = EmpresaIdAtual();

            return _context.Produtos
                .AsNoTracking()
                .CountAsync(p => p.CategoriaProdutoId == categoriaId && p.EmpresaId == empresaId);
        }

        private async Task PreencherVinculosAsync(
            IReadOnlyList<CategoriaProdutoLinhaViewModel> categorias, int empresaId)
        {
            if (categorias.Count == 0)
                return;

            var ids = categorias.Select(c => c.Id).ToList();

            var produtosPorCategoria = await _context.Produtos
                .AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && ids.Contains(p.CategoriaProdutoId))
                .GroupBy(p => p.CategoriaProdutoId)
                .Select(grupo => new { CategoriaId = grupo.Key, Total = grupo.Count() })
                .ToDictionaryAsync(x => x.CategoriaId, x => x.Total);

            foreach (var categoria in categorias)
            {
                categoria.QuantidadeProdutos =
                    produtosPorCategoria.TryGetValue(categoria.Id, out var produtos) ? produtos : 0;
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
                 WHERE coluna.object_id = OBJECT_ID('Tb_Categoria_Produto')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao, "Não foi possível prever o próximo id de Tb_Categoria_Produto.");
                return null;
            }
        }

        private static CategoriaProdutoFormViewModel ParaFormulario(CategoriaProduto categoria) => new()
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Descricao = categoria.Descricao
        };
    }
}
