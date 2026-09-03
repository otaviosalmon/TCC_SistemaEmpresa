using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCC_SistemaEmpresa.Models;

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
            if (User.IsInRole("ADMIN") || User.IsInRole("GERENTE"))
                return RedirectToAction("Index", "Dashboard");

            return RedirectToAction("Index", "Vendas");
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
