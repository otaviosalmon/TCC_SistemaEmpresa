using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models.ViewModels;
using TCC_SistemaEmpresa.Security;
using TCC_SistemaEmpresa.Services;

namespace TCC_SistemaEmpresa.Controllers
{
    public class ConfiguracoesController : Controller
    {
        private readonly AppDbContext _context;

        public ConfiguracoesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new ConfiguracoesViewModel
            {
                Tema = Aparencia.Tema.Atual(Request),
                Fonte = Aparencia.Fonte.Atual(Request),
                Densidade = Aparencia.Densidade.Atual(Request),
                Empresa = User.FindFirstValue(ClaimsEmpresa.EmpresaNome) ?? string.Empty,
                Usuario = User.Identity?.Name ?? string.Empty,
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                Perfil = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty
            };

            if (User.IsInRole("ADMIN"))
                model.DadosEmpresa = await BuscarEmpresaAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SalvarAparencia(string? tema, string? fonte, string? densidade)
        {
            Aparencia.Tema.Gravar(Response, tema);
            Aparencia.Fonte.Gravar(Response, fonte);
            Aparencia.Densidade.Gravar(Response, densidade);

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                return NoContent();

            return RedirectToAction(nameof(Index));
        }

        private async Task<EmpresaResumoViewModel?> BuscarEmpresaAsync()
        {
            var empresaId = int.TryParse(User.FindFirstValue(ClaimsEmpresa.EmpresaId), out var id) ? id : 0;

            return await _context.Empresa
                .AsNoTracking()
                .Where(e => e.Id == empresaId)
                .Select(e => new EmpresaResumoViewModel
                {
                    Nome = e.Nome,
                    Cnpj = e.Cnpj,
                    Email = e.Email,
                    Telefone = e.Telefone,
                    Endereco = e.Endereco,
                    Cidade = e.Cidade,
                    Estado = e.Estado,
                    Cep = e.Cep,
                    Ativo = e.Ativo
                })
                .FirstOrDefaultAsync();
        }
    }
}
