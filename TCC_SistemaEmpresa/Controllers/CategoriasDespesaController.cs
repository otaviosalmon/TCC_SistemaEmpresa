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
    public class CategoriasDespesaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoriasDespesaController> _logger;

        public CategoriasDespesaController(AppDbContext context, ILogger<CategoriasDespesaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? busca, string? situacao)
        {
            var empresaId = EmpresaIdAtual();
            situacao = NormalizarSituacao(situacao);

            var consulta = _context.CategoriasDespesa
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

            var categorias = await consulta
                .OrderBy(c => c.Nome)
                .Select(c => new CategoriaDespesaLinhaViewModel
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Ativo = c.Ativo
                })
                .ToListAsync();

            await PreencherVinculosAsync(categorias, empresaId);

            return View(new CategoriaDespesaListaViewModel
            {
                Busca = busca,
                Situacao = situacao,
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
            model.QuantidadeDespesas = await ContarDespesasAsync(id);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CategoriaDespesaFormViewModel
            {
                Ativo = true,
                ProximoId = await ProximoIdAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoriaDespesaFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                return View(model);
            }

            var categoria = new CategoriaDespesa
            {
                EmpresaId = empresaId,
                Nome = model.Nome.Trim(),
                Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim(),
                Ativo = model.Ativo
            };

            _context.CategoriasDespesa.Add(categoria);
            await _context.SaveChangesAsync();

            RegistrarLog("CRIACAO", categoria.Id, $"Categoria de despesa '{categoria.Nome}' criada.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Categoria de despesa {CategoriaId} criada na empresa {EmpresaId}.",
                categoria.Id, empresaId);

            TempData["Sucesso"] = $"Categoria de despesa {categoria.Nome} cadastrada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var categoria = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (categoria is null)
                return NotFound();

            var model = ParaFormulario(categoria);
            model.QuantidadeDespesas = await ContarDespesasAsync(id);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, CategoriaDespesaFormViewModel model)
        {
            var categoria = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (categoria is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.QuantidadeDespesas = await ContarDespesasAsync(id);
                return View(model);
            }

            var estavaAtiva = categoria.Ativo;

            categoria.Nome = model.Nome.Trim();
            categoria.Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim();
            categoria.Ativo = model.Ativo;

            var (acao, detalhe) = (estavaAtiva, model.Ativo) switch
            {
                (true, false) => ("INATIVACAO", "inativada"),
                (false, true) => ("REATIVACAO", "reativada"),
                _ => ("ALTERACAO", "alterada")
            };

            RegistrarLog(acao, categoria.Id, $"Categoria de despesa '{categoria.Nome}' {detalhe}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Categoria de despesa {CategoriaId} da empresa {EmpresaId}: {Acao}.",
                categoria.Id, empresaId, acao);

            TempData["Sucesso"] = $"Categoria de despesa {categoria.Nome} {detalhe} com sucesso.";
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

            if (categoria.Ativo)
            {
                TempData["Erro"] = $"A categoria {nome} precisa ser inativada antes de ser excluída.";
                return RedirectToAction(nameof(Index));
            }

            var quantidadeDespesas = await ContarDespesasAsync(id);

            if (quantidadeDespesas > 0)
            {
                TempData["Erro"] =
                    $"A categoria {nome} classifica {quantidadeDespesas} despesa(s) e não pode ser excluída.";
                return RedirectToAction(nameof(Index));
            }

            RegistrarLog("EXCLUSAO", categoria.Id, $"Categoria de despesa '{nome}' excluída definitivamente.");

            _context.CategoriasDespesa.Remove(categoria);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Categoria de despesa {CategoriaId} ({Nome}) excluída definitivamente por {Usuario}.",
                id, nome, User.Identity?.Name);

            TempData["Sucesso"] = $"Categoria de despesa {nome} foi excluída definitivamente.";
            return RedirectToAction(nameof(Index));
        }

        private Task<CategoriaDespesa?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.CategoriasDespesa.AsTracking()
                : _context.CategoriasDespesa.AsNoTracking();

            return consulta.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        }

        private Task<int> ContarDespesasAsync(int categoriaId)
        {
            var empresaId = EmpresaIdAtual();

            return _context.Despesas
                .AsNoTracking()
                .CountAsync(d => d.CategoriaDespesaId == categoriaId && d.EmpresaId == empresaId);
        }

        private async Task PreencherVinculosAsync(
            IReadOnlyList<CategoriaDespesaLinhaViewModel> categorias, int empresaId)
        {
            if (categorias.Count == 0)
                return;

            var ids = categorias.Select(c => c.Id).ToList();

            var despesasPorCategoria = await _context.Despesas
                .AsNoTracking()
                .Where(d => d.EmpresaId == empresaId && ids.Contains(d.CategoriaDespesaId))
                .GroupBy(d => d.CategoriaDespesaId)
                .Select(grupo => new { CategoriaId = grupo.Key, Total = grupo.Count() })
                .ToDictionaryAsync(x => x.CategoriaId, x => x.Total);

            foreach (var categoria in categorias)
            {
                categoria.QuantidadeDespesas =
                    despesasPorCategoria.TryGetValue(categoria.Id, out var despesas) ? despesas : 0;
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
                 WHERE coluna.object_id = OBJECT_ID('Tb_Categoria_Despesa')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao, "Não foi possível prever o próximo id de Tb_Categoria_Despesa.");
                return null;
            }
        }

        private static CategoriaDespesaFormViewModel ParaFormulario(CategoriaDespesa categoria) => new()
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Descricao = categoria.Descricao,
            Ativo = categoria.Ativo
        };

        private void RegistrarLog(string acao, int registroId, string detalhes)
        {
            _context.LogsSistema.Add(new LogSistema
            {
                EmpresaId = EmpresaIdAtual(),
                UsuarioId = UsuarioIdAtual(),
                Acao = acao,
                EntidadeAfetada = nameof(CategoriaDespesa),
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
