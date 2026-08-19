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
    public class DespesasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DespesasController> _logger;

        public DespesasController(AppDbContext context, ILogger<DespesasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? busca, string? recorrencia)
        {
            var empresaId = EmpresaIdAtual();
            recorrencia = NormalizarRecorrencia(recorrencia);

            var consulta = _context.Despesas
                .AsNoTracking()
                .Where(d => d.EmpresaId == empresaId);

            consulta = recorrencia switch
            {
                RecorrenciaFiltro.Fixas => consulta.Where(d => d.Fixa),
                RecorrenciaFiltro.Eventuais => consulta.Where(d => !d.Fixa),
                _ => consulta
            };

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(d =>
                    d.CategoriaDespesa.Nome.Contains(termo)
                    || (d.Descricao != null && d.Descricao.Contains(termo)));
            }

            var despesas = await consulta
                .OrderByDescending(d => d.DataDespesa)
                .ThenByDescending(d => d.Id)
                .Select(d => new DespesaLinhaViewModel
                {
                    Id = d.Id,
                    Categoria = d.CategoriaDespesa.Nome,
                    Valor = d.Valor,
                    DataDespesa = d.DataDespesa,
                    Fixa = d.Fixa,
                    Descricao = d.Descricao
                })
                .ToListAsync();

            return View(new DespesaListaViewModel
            {
                Busca = busca,
                Recorrencia = recorrencia,
                Despesas = despesas
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var despesa = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (despesa is null)
                return NotFound();

            var model = ParaFormulario(despesa);
            model.SomenteLeitura = true;
            model.Categorias = await CarregarCategoriasAsync(despesa.CategoriaDespesaId);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new DespesaFormViewModel
            {
                DataDespesa = DateTime.Today,
                ProximoId = await ProximoIdAsync(),
                Categorias = await CarregarCategoriasAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DespesaFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();
            var usuarioId = UsuarioIdAtual();

            await ValidarRegrasAsync(model, empresaId);

            if (usuarioId is null)
            {
                ModelState.AddModelError(string.Empty,
                    "Não foi possível identificar o usuário da sessão para registrar o lançamento. Refaça o login.");
            }

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                model.Categorias = await CarregarCategoriasAsync(model.CategoriaDespesaId);
                return View(model);
            }

            var despesa = new Despesa
            {
                EmpresaId = empresaId,
                CategoriaDespesaId = model.CategoriaDespesaId,
                UsuarioId = usuarioId!.Value,
                Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim(),
                Valor = model.Valor!.Value,
                DataDespesa = model.DataDespesa.Date,
                Fixa = model.Fixa!.Value
            };

            _context.Despesas.Add(despesa);
            await _context.SaveChangesAsync();

            RegistrarLog("CRIACAO", despesa.Id,
                $"Despesa de {despesa.Valor:C} lançada em {despesa.DataDespesa:dd/MM/yyyy}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Despesa {DespesaId} lançada na empresa {EmpresaId} pelo usuário {UsuarioId}.",
                despesa.Id, empresaId, usuarioId);

            TempData["Sucesso"] = $"Despesa de {despesa.Valor:C} lançada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var despesa = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (despesa is null)
                return NotFound();

            var model = ParaFormulario(despesa);
            model.Categorias = await CarregarCategoriasAsync(despesa.CategoriaDespesaId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, DespesaFormViewModel model)
        {
            var despesa = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (despesa is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();

            await ValidarRegrasAsync(model, empresaId);

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.Categorias = await CarregarCategoriasAsync(model.CategoriaDespesaId);
                return View(model);
            }

            despesa.CategoriaDespesaId = model.CategoriaDespesaId;
            despesa.Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim();
            despesa.Valor = model.Valor!.Value;
            despesa.DataDespesa = model.DataDespesa.Date;
            despesa.Fixa = model.Fixa!.Value;

            RegistrarLog("ALTERACAO", despesa.Id,
                $"Despesa alterada para {despesa.Valor:C} em {despesa.DataDespesa:dd/MM/yyyy}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Despesa {DespesaId} da empresa {EmpresaId} alterada.", despesa.Id, empresaId);

            TempData["Sucesso"] = $"Despesa de {despesa.Valor:C} alterada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromRoute] int id)
        {
            var despesa = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (despesa is null)
                return NotFound();

            var valor = despesa.Valor;
            var data = despesa.DataDespesa;

            RegistrarLog("EXCLUSAO", despesa.Id,
                $"Despesa de {valor:C} de {data:dd/MM/yyyy} excluída definitivamente.");

            _context.Despesas.Remove(despesa);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Despesa {DespesaId} ({Valor}) excluída definitivamente por {Usuario}.",
                id, valor, User.Identity?.Name);

            TempData["Sucesso"] = $"Despesa de {valor:C} foi excluída definitivamente.";
            return RedirectToAction(nameof(Index));
        }

        private Task<Despesa?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.Despesas.AsTracking()
                : _context.Despesas.AsNoTracking();

            return consulta.FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId);
        }

        private async Task ValidarRegrasAsync(DespesaFormViewModel model, int empresaId)
        {
            if (model.CategoriaDespesaId > 0)
            {
                var categoriaValida = await _context.CategoriasDespesa
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == model.CategoriaDespesaId && c.EmpresaId == empresaId);

                if (!categoriaValida)
                    ModelState.AddModelError(nameof(model.CategoriaDespesaId), "Categoria de despesa inválida.");
            }

            if (model.DataDespesa.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.DataDespesa),
                    "A data da despesa não pode ser futura.");
            }
        }

        private async Task<IEnumerable<SelectListItem>> CarregarCategoriasAsync(int? selecionado = null)
        {
            var empresaId = EmpresaIdAtual();

            var categorias = await _context.CategoriasDespesa
                .AsNoTracking()
                .Where(c => c.EmpresaId == empresaId
                    && (c.Ativo || (selecionado.HasValue && c.Id == selecionado.Value)))
                .OrderBy(c => c.Nome)
                .Select(c => new { c.Id, c.Nome, c.Ativo })
                .ToListAsync();

            return categorias.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Ativo ? c.Nome : $"{c.Nome} (inativa)",
                Selected = selecionado.HasValue && c.Id == selecionado.Value
            });
        }

        private async Task<int?> ProximoIdAsync()
        {
            const string sql = @"
                SELECT CASE
                           WHEN coluna.last_value IS NULL THEN CONVERT(int, coluna.seed_value)
                           ELSE CONVERT(int, coluna.last_value) + CONVERT(int, coluna.increment_value)
                       END AS Value
                  FROM sys.identity_columns AS coluna
                 WHERE coluna.object_id = OBJECT_ID('Tb_Despesa')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao, "Não foi possível prever o próximo id de Tb_Despesa.");
                return null;
            }
        }

        private static DespesaFormViewModel ParaFormulario(Despesa despesa) => new()
        {
            Id = despesa.Id,
            CategoriaDespesaId = despesa.CategoriaDespesaId,
            DataDespesa = despesa.DataDespesa,
            Fixa = despesa.Fixa,
            Valor = despesa.Valor,
            Descricao = despesa.Descricao
        };

        private void RegistrarLog(string acao, int registroId, string detalhes)
        {
            _context.LogsSistema.Add(new LogSistema
            {
                EmpresaId = EmpresaIdAtual(),
                UsuarioId = UsuarioIdAtual(),
                Acao = acao,
                EntidadeAfetada = nameof(Despesa),
                RegistroId = registroId,
                DataHora = DateTime.Now,
                Detalhes = detalhes
            });
        }

        private static string NormalizarRecorrencia(string? recorrencia) => recorrencia switch
        {
            RecorrenciaFiltro.Fixas => RecorrenciaFiltro.Fixas,
            RecorrenciaFiltro.Eventuais => RecorrenciaFiltro.Eventuais,
            _ => RecorrenciaFiltro.Todas
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
