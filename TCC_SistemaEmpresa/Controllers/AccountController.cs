using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models.ViewModels;
using TCC_SistemaEmpresa.Security;

namespace TCC_SistemaEmpresa.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction(nameof(HomeController.Index), "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var username = model.Usuario.Trim();

            var usuario = await _context.Usuario
                .AsNoTracking()
                .Include(u => u.Empresa)
                .FirstOrDefaultAsync(u => u.Username == username && u.Ativo);


            var senhaConfere = usuario is not null
                && PasswordHasher.Verificar(usuario.Username, model.Senha, usuario.PasswordHash);

            if (!senhaConfere)
            {
                _logger.LogWarning("Falha de autenticação para o usuário {Username}.", username);
                ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario!.Id.ToString()),
                new(ClaimTypes.Name, usuario.Username),
                new(ClaimTypes.Role, usuario.Role),
                new(ClaimTypes.Email, usuario.Email ?? string.Empty),
                new(ClaimsEmpresa.EmpresaId, usuario.EmpresaId.ToString()),
                new(ClaimsEmpresa.EmpresaNome, usuario.Empresa?.Nome ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false });

            _logger.LogInformation(
                "Usuário {Username} (Id {UsuarioId}, empresa {EmpresaId}) autenticado.",
                usuario.Username, usuario.Id, usuario.EmpresaId);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AcessoNegado() => View();
    }
}
