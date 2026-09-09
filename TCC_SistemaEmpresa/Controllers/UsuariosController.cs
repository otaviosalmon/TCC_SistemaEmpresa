using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;
using TCC_SistemaEmpresa.Security;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = RolesUsuario.Admin)]
    public class UsuariosController : ControllerValidacao
    {
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(AppDbContext context, ILogger<UsuariosController> logger)
            : base(context)
        {
            _logger = logger;
        }

        protected override string EntidadeLog => nameof(Usuario);

        [HttpGet]
        public async Task<IActionResult> Index(string? busca, string? situacao, string? role, int pagina = 1)
        {
            var empresaId = EmpresaIdAtual();
            var usuarioAtual = UsuarioIdAtual();

            situacao = NormalizarSituacao(situacao);
            role = NormalizarRole(role);

            var consulta = _context.Usuario
                .AsNoTracking()
                .Where(u => u.EmpresaId == empresaId);

            consulta = situacao switch
            {
                SituacaoFiltro.Ativos => consulta.Where(u => u.Ativo),
                SituacaoFiltro.Inativos => consulta.Where(u => !u.Ativo),
                _ => consulta
            };

            if (role != RolesUsuario.Todos)
                consulta = consulta.Where(u => u.Role == role);

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();

                consulta = consulta.Where(u => u.Username.Contains(termo)
                                            || (u.Email != null && u.Email.Contains(termo)));
            }

            var paginacao = PaginacaoViewModel.Criar(pagina, await consulta.CountAsync());

            var usuarios = await consulta
                .OrderBy(u => u.Username)
                .Pagina(paginacao)
                .Select(u => new UsuarioLinhaViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    Ativo = u.Ativo,
                    DataCadastro = u.DataCadastro
                })
                .ToListAsync();

            var ids = usuarios.Select(u => u.Id).ToList();
            var vinculos = await ContarVinculosAsync(ids, empresaId);

            foreach (var usuario in usuarios)
            {
                usuario.QuantidadeVinculos =
                    vinculos.TryGetValue(usuario.Id, out var total) ? total : 0;

                usuario.EhUsuarioAtual = usuarioAtual == usuario.Id;
            }

            return View(new UsuarioListaViewModel
            {
                Busca = busca,
                Situacao = situacao,
                Role = role,
                Paginacao = paginacao,
                Usuarios = usuarios
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var usuario = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (usuario is null)
                return NotFound();

            var model = ParaFormulario(usuario);
            model.SomenteLeitura = true;
            model.EhUsuarioAtual = UsuarioIdAtual() == id;
            model.Roles = CarregarRoles(usuario.Role);
            model.FuncionarioVinculado = await BuscarFuncionarioVinculadoAsync(id, usuario.EmpresaId);

            var vinculos = await ContarVinculosAsync(new[] { id }, usuario.EmpresaId);
            model.QuantidadeVinculos = vinculos.TryGetValue(id, out var total) ? total : 0;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new UsuarioFormViewModel
            {
                Ativo = true,
                DataCadastro = DateTime.Now,
                ProximoId = await ProximoIdAsync(),
                Roles = CarregarRoles(RolesUsuario.Vendedor)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();
            var username = (model.Username ?? string.Empty).Trim();
            var email = (model.Email ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(model.Senha))
                ModelState.AddModelError(nameof(model.Senha), "Informe a senha inicial do usuário.");

            await ValidarRegrasAsync(model, username, email, empresaId, usuarioId: null);

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                model.Roles = CarregarRoles(model.Role);
                return View(model);
            }

            var usuario = new Usuario
            {
                EmpresaId = empresaId,
                Username = username,
                Email = email,
                Role = model.Role,
                Ativo = model.Ativo,
                DataCadastro = DateTime.Now,
                PasswordHash = PasswordHasher.GerarHash(username, model.Senha!)
            };

            await using var transacao = await _context.Database.BeginTransactionAsync();

            _context.Usuario.Add(usuario);
            await _context.SaveChangesAsync();

            RegistrarLog("CRIACAO", usuario.Id,
                $"Usuário '{usuario.Username}' criado com perfil {usuario.Role}.");
            await _context.SaveChangesAsync();

            await transacao.CommitAsync();

            _logger.LogInformation(
                "Usuário {UsuarioId} criado na empresa {EmpresaId} com perfil {Role}.",
                usuario.Id, empresaId, usuario.Role);

            TempData["Sucesso"] = $"Usuário {usuario.Username} cadastrado com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var usuario = await BuscarDaEmpresaAsync(id, rastrear: false);
            if (usuario is null)
                return NotFound();

            var model = ParaFormulario(usuario);
            model.EhUsuarioAtual = UsuarioIdAtual() == id;
            model.Roles = CarregarRoles(usuario.Role);
            model.FuncionarioVinculado = await BuscarFuncionarioVinculadoAsync(id, usuario.EmpresaId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, UsuarioFormViewModel model)
        {
            var usuario = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (usuario is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();
            var ehUsuarioAtual = UsuarioIdAtual() == id;

            var username = (model.Username ?? string.Empty).Trim();
            var email = (model.Email ?? string.Empty).Trim();

            var usernameOriginal = usuario.Username;
            var roleOriginal = usuario.Role;
            var estavaAtivo = usuario.Ativo;

            var trocouUsername = !string.Equals(username, usernameOriginal,
                StringComparison.OrdinalIgnoreCase);

            await ValidarRegrasAsync(model, username, email, empresaId, usuarioId: id);

            if (trocouUsername && string.IsNullOrWhiteSpace(model.Senha))
            {
                ModelState.AddModelError(nameof(model.Senha),
                    "O login faz parte do salt da senha. Ao trocar o usuário é obrigatório definir uma nova senha.");
            }

            if (ehUsuarioAtual && !model.Ativo)
            {
                ModelState.AddModelError(nameof(model.Ativo),
                    "Você não pode inativar o próprio usuário.");
            }

            if (ehUsuarioAtual && model.Role != roleOriginal)
            {
                ModelState.AddModelError(nameof(model.Role),
                    "Você não pode alterar o próprio perfil de acesso.");
            }

            if (roleOriginal == RolesUsuario.Admin && estavaAtivo
                && !(model.Ativo && model.Role == RolesUsuario.Admin))
            {
                var outrosAdmins = await _context.Usuario
                    .AsNoTracking()
                    .CountAsync(u => u.EmpresaId == empresaId
                                  && u.Id != id
                                  && u.Ativo
                                  && u.Role == RolesUsuario.Admin);

                if (outrosAdmins == 0)
                {
                    ModelState.AddModelError(string.Empty,
                        "Esta empresa ficaria sem nenhum administrador ativo. Promova outro usuário antes de alterar este.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.DataCadastro = usuario.DataCadastro;
                model.EhUsuarioAtual = ehUsuarioAtual;
                model.Roles = CarregarRoles(model.Role);
                model.FuncionarioVinculado = await BuscarFuncionarioVinculadoAsync(id, empresaId);
                return View(model);
            }

            usuario.Username = username;
            usuario.Email = email;
            usuario.Role = model.Role;
            usuario.Ativo = model.Ativo;

            var trocouSenha = !string.IsNullOrWhiteSpace(model.Senha);

            if (trocouSenha)
                usuario.PasswordHash = PasswordHasher.GerarHash(username, model.Senha!);

            var (acao, detalhe) = (estavaAtivo, model.Ativo) switch
            {
                (true, false) => ("INATIVACAO", "inativado"),
                (false, true) => ("REATIVACAO", "reativado"),
                _ => ("ALTERACAO", "alterado")
            };

            var complemento = new List<string>();

            if (trocouUsername)
                complemento.Add($"login alterado de '{usernameOriginal}' para '{username}'");

            if (model.Role != roleOriginal)
                complemento.Add($"perfil alterado de {roleOriginal} para {model.Role}");

            if (trocouSenha)
                complemento.Add("senha redefinida");

            var textoComplemento = complemento.Count == 0
                ? string.Empty
                : " (" + string.Join("; ", complemento) + ")";

            RegistrarLog(acao, usuario.Id, $"Usuário '{username}' {detalhe}{textoComplemento}.");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Usuário {UsuarioId} da empresa {EmpresaId}: {Acao}.",
                usuario.Id, empresaId, acao);

            TempData["Sucesso"] = $"Usuário {username} {detalhe} com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromRoute] int id)
        {
            var usuario = await BuscarDaEmpresaAsync(id, rastrear: true);
            if (usuario is null)
                return NotFound();

            var empresaId = EmpresaIdAtual();
            var username = usuario.Username;

            if (UsuarioIdAtual() == id)
            {
                TempData["Erro"] = "Você não pode excluir o próprio usuário.";
                return RedirectToAction(nameof(Index));
            }

            if (usuario.Ativo)
            {
                TempData["Erro"] = $"O usuário {username} precisa ser inativado antes de ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            var vinculos = await ContarVinculosAsync(new[] { id }, empresaId);
            var total = vinculos.TryGetValue(id, out var quantidade) ? quantidade : 0;

            if (total > 0)
            {
                TempData["Erro"] =
                    $"O usuário {username} possui {total} registro(s) vinculado(s) (logs, movimentações, despesas ou funcionário) e não pode ser excluído.";
                return RedirectToAction(nameof(Index));
            }

            RegistrarLog("EXCLUSAO", usuario.Id,
                $"Usuário '{username}' (perfil {usuario.Role}) excluído definitivamente.");
            await _context.SaveChangesAsync();

            _context.Usuario.Remove(usuario);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Usuário {UsuarioId} ({Username}) excluído definitivamente por {Operador}.",
                id, username, User.Identity?.Name);

            TempData["Sucesso"] = $"Usuário {username} foi excluído definitivamente.";
            return RedirectToAction(nameof(Index));
        }

        private Task<Usuario?> BuscarDaEmpresaAsync(int id, bool rastrear)
        {
            var empresaId = EmpresaIdAtual();

            var consulta = rastrear
                ? _context.Usuario.AsTracking()
                : _context.Usuario.AsNoTracking();

            return consulta.FirstOrDefaultAsync(u => u.Id == id && u.EmpresaId == empresaId);
        }

        private async Task ValidarRegrasAsync(
            UsuarioFormViewModel model, string username, string email, int empresaId, int? usuarioId)
        {
            if (!RolesUsuario.EhValido(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role),
                    "Perfil de acesso inválido para este sistema.");
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                var loginEmUso = await _context.Usuario
                    .AsNoTracking()
                    .AnyAsync(u => u.EmpresaId == empresaId
                                && u.Username == username
                                && (usuarioId == null || u.Id != usuarioId));

                if (loginEmUso)
                {
                    ModelState.AddModelError(nameof(model.Username),
                        "Já existe um usuário com este login nesta empresa.");
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailEmUso = await _context.Usuario
                    .AsNoTracking()
                    .AnyAsync(u => u.EmpresaId == empresaId
                                && u.Email == email
                                && (usuarioId == null || u.Id != usuarioId));

                if (emailEmUso)
                {
                    ModelState.AddModelError(nameof(model.Email),
                        "Já existe um usuário com este e-mail nesta empresa.");
                }
            }
        }

        private async Task<Dictionary<int, int>> ContarVinculosAsync(
            IReadOnlyCollection<int> ids, int empresaId)
        {
            var vinculos = ids.Distinct().ToDictionary(id => id, _ => 0);

            if (vinculos.Count == 0)
                return vinculos;

            var chaves = ids.ToList();

            async Task AcumularAsync(IQueryable<int> consulta)
            {
                var contagens = await consulta
                    .GroupBy(usuarioId => usuarioId)
                    .Select(grupo => new { UsuarioId = grupo.Key, Total = grupo.Count() })
                    .ToListAsync();

                foreach (var contagem in contagens)
                {
                    if (vinculos.ContainsKey(contagem.UsuarioId))
                        vinculos[contagem.UsuarioId] += contagem.Total;
                }
            }

            await AcumularAsync(_context.LogsSistema
                .AsNoTracking()
                .Where(l => l.EmpresaId == empresaId
                         && l.UsuarioId != null
                         && chaves.Contains(l.UsuarioId.Value))
                .Select(l => l.UsuarioId!.Value));

            await AcumularAsync(_context.MovimentacoesEstoque
                .AsNoTracking()
                .Where(m => m.EmpresaId == empresaId && chaves.Contains(m.UsuarioId))
                .Select(m => m.UsuarioId));

            await AcumularAsync(_context.Despesas
                .AsNoTracking()
                .Where(d => d.EmpresaId == empresaId && chaves.Contains(d.UsuarioId))
                .Select(d => d.UsuarioId));

            await AcumularAsync(_context.Funcionarios
                .AsNoTracking()
                .Where(f => f.EmpresaId == empresaId
                         && f.UsuarioId != null
                         && chaves.Contains(f.UsuarioId.Value))
                .Select(f => f.UsuarioId!.Value));

            return vinculos;
        }

        private Task<string?> BuscarFuncionarioVinculadoAsync(int usuarioId, int empresaId) =>
            _context.Funcionarios
                .AsNoTracking()
                .Where(f => f.EmpresaId == empresaId && f.UsuarioId == usuarioId)
                .OrderBy(f => f.Nome)
                .Select(f => (string?)f.Nome)
                .FirstOrDefaultAsync();

        private async Task<int?> ProximoIdAsync()
        {
            const string sql = @"
                SELECT CASE
                           WHEN coluna.last_value IS NULL THEN CONVERT(int, coluna.seed_value)
                           ELSE CONVERT(int, coluna.last_value) + CONVERT(int, coluna.increment_value)
                       END AS Value
                  FROM sys.identity_columns AS coluna
                 WHERE coluna.object_id = OBJECT_ID('Tb_Usuario')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception excecao)
            {
                _logger.LogWarning(excecao,
                    "Não foi possível prever o próximo id de Tb_Usuario.");
                return null;
            }
        }

        private static IEnumerable<SelectListItem> CarregarRoles(string? selecionado = null) =>
            RolesUsuario.Aceitos.Select(role => new SelectListItem
            {
                Value = role,
                Text = RolesUsuario.Rotulo(role),
                Selected = role == selecionado
            });

        private static UsuarioFormViewModel ParaFormulario(Usuario usuario) => new()
        {
            Id = usuario.Id,
            Username = usuario.Username,
            Email = usuario.Email ?? string.Empty,
            Role = usuario.Role,
            Ativo = usuario.Ativo,
            DataCadastro = usuario.DataCadastro
        };

        private static string NormalizarSituacao(string? situacao) => situacao switch
        {
            SituacaoFiltro.Ativos => SituacaoFiltro.Ativos,
            SituacaoFiltro.Inativos => SituacaoFiltro.Inativos,
            _ => SituacaoFiltro.Todos
        };

        private static string NormalizarRole(string? role) =>
            RolesUsuario.EhValido(role) ? role! : RolesUsuario.Todos;
    }
}