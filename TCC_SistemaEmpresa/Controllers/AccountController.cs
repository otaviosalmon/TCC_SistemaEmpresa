using Microsoft.AspNetCore.Mvc;

namespace TCC_SistemaEmpresa.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}
