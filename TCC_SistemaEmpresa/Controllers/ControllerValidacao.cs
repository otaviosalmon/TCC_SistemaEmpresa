using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;

namespace TCC_SistemaEmpresa.Controllers
{
    public abstract class ControllerValidacao : Controller
    {
        protected readonly AppDbContext _context;

        protected ControllerValidacao(AppDbContext context)
        {
            _context = context;
        }

        protected abstract string EntidadeLog { get; }

        protected int EmpresaIdAtual()
        {
            var claim = User.FindFirstValue(Security.ClaimsEmpresa.EmpresaId);
            return int.TryParse(claim, out var empresaId) ? empresaId : 0;
        }

        protected int? UsuarioIdAtual()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var usuarioId) ? usuarioId : null;
        }

        protected void RegistrarLog(string acao, int? registroId, string detalhes)
        {
            _context.LogsSistema.Add(new LogSistema
            {
                EmpresaId = EmpresaIdAtual(),
                UsuarioId = UsuarioIdAtual(),
                Acao = acao,
                EntidadeAfetada = EntidadeLog,
                RegistroId = registroId,
                DataHora = DateTime.Now,
                Detalhes = detalhes
            });
        }
    }
}
