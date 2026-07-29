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

        // Destino padrão após o login (rota default: {controller=Home}/{action=Index}).
        public IActionResult Index()
        {
            return View();
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

        // Anônimo: a página de erro precisa responder mesmo sem usuário logado,
        // senão uma falha em quem não está autenticado vira redirect para o login.
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
