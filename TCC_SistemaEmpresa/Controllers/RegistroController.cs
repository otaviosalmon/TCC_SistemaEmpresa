using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = RolesUsuario.Admin)]
    public class RegistrosController : ControllerValidacao
    {
        public RegistrosController(AppDbContext context) : base(context)
        {
        }

        protected override string EntidadeLog => nameof(LogSistema);

        [HttpGet]
        public async Task<IActionResult> Index(
            string? busca,
            string? entidade,
            string? acao,
            DateTime? dataInicial,
            DateTime? dataFinal,
            int pagina = 1)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = _context.LogsSistema
                .AsNoTracking()
                .Where(l => l.EmpresaId == empresaId);

            if (dataInicial.HasValue)
                consulta = consulta.Where(l => l.DataHora >= dataInicial.Value.Date);

            if (dataFinal.HasValue)
            {
                var limite = dataFinal.Value.Date.AddDays(1);
                consulta = consulta.Where(l => l.DataHora < limite);
            }

            if (!string.IsNullOrWhiteSpace(entidade))
                consulta = consulta.Where(l => l.EntidadeAfetada == entidade);

            if (!string.IsNullOrWhiteSpace(acao))
                consulta = consulta.Where(l => l.Acao == acao);

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();

                consulta = consulta.Where(l => l.EntidadeAfetada.Contains(termo)
                                            || l.Acao.Contains(termo)
                                            || (l.Detalhes != null && l.Detalhes.Contains(termo))
                                            || (l.Usuario != null && l.Usuario.Username.Contains(termo)));
            }

            var paginacao = PaginacaoViewModel.Criar(pagina, await consulta.CountAsync());

            var registros = await consulta
                .OrderByDescending(l => l.DataHora)
                .ThenByDescending(l => l.Id)
                .Pagina(paginacao)
                .Select(l => new RegistroLinhaViewModel
                {
                    Id = l.Id,
                    UsuarioId = l.UsuarioId,
                    Usuario = l.Usuario != null ? l.Usuario.Username : null,
                    Acao = l.Acao,
                    EntidadeAfetada = l.EntidadeAfetada,
                    RegistroId = l.RegistroId,
                    DataHora = l.DataHora,
                    Detalhes = l.Detalhes
                })
                .ToListAsync();

            return View(new RegistroListaViewModel
            {
                Busca = busca,
                Entidade = entidade,
                Acao = acao,
                DataInicial = dataInicial,
                DataFinal = dataFinal,
                Entidades = await CarregarEntidadesAsync(empresaId, entidade),
                Acoes = await CarregarAcoesAsync(empresaId, acao),
                Paginacao = paginacao,
                Registros = registros
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] long id)
        {
            var empresaId = EmpresaIdAtual();

            var registro = await _context.LogsSistema
                .AsNoTracking()
                .Where(l => l.Id == id && l.EmpresaId == empresaId)
                .Select(l => new RegistroLinhaViewModel
                {
                    Id = l.Id,
                    UsuarioId = l.UsuarioId,
                    Usuario = l.Usuario != null ? l.Usuario.Username : null,
                    Acao = l.Acao,
                    EntidadeAfetada = l.EntidadeAfetada,
                    RegistroId = l.RegistroId,
                    DataHora = l.DataHora,
                    Detalhes = l.Detalhes
                })
                .FirstOrDefaultAsync();

            if (registro is null)
                return NotFound();

            return View(registro);
        }

        private async Task<IEnumerable<SelectListItem>> CarregarEntidadesAsync(
            int empresaId, string? selecionada)
        {
            var entidades = await _context.LogsSistema
                .AsNoTracking()
                .Where(l => l.EmpresaId == empresaId)
                .Select(l => l.EntidadeAfetada)
                .Distinct()
                .OrderBy(nome => nome)
                .ToListAsync();

            return entidades.Select(nome => new SelectListItem
            {
                Value = nome,
                Text = nome,
                Selected = nome == selecionada
            });
        }

        private async Task<IEnumerable<SelectListItem>> CarregarAcoesAsync(
            int empresaId, string? selecionada)
        {
            var acoes = await _context.LogsSistema
                .AsNoTracking()
                .Where(l => l.EmpresaId == empresaId)
                .Select(l => l.Acao)
                .Distinct()
                .OrderBy(nome => nome)
                .ToListAsync();

            return acoes.Select(nome => new SelectListItem
            {
                Value = nome,
                Text = nome,
                Selected = nome == selecionada
            });
        }
    }
}