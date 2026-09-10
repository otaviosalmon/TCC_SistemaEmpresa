using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;

namespace TCC_SistemaEmpresa.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.IsInRole(RolesUsuario.Admin) || User.IsInRole(RolesUsuario.Gerente))
                return RedirectToAction("Index", "Dashboard");

            if (User.IsInRole(RolesUsuario.Estoquista))
                return RedirectToAction("Index", "Movimentacoes");

            if (User.IsInRole(RolesUsuario.Vendedor) || User.IsInRole(RolesUsuario.Caixa))
                return RedirectToAction("Index", "Vendas");

            return RedirectToAction("AcessoNegado", "Account");
        }

        public IActionResult Teste()
        {
            ViewBag.Mensagem = "O Sistema Está Funcionando!";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
