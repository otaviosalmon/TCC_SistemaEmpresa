namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public static class RolesUsuario
    {
        public const string Admin = "ADMIN";
        public const string Gerente = "GERENTE";
        public const string Vendedor = "VENDEDOR";
        public const string Caixa = "CAIXA";
        public const string Estoquista = "ESTOQUISTA";

        public const string Todos = "todos";

        public static readonly IReadOnlyList<string> Aceitos = new[]
        {
            Admin, Gerente, Vendedor, Caixa, Estoquista
        };

        public static bool EhValido(string? role) =>
            !string.IsNullOrWhiteSpace(role) && Aceitos.Contains(role);

        public static string Rotulo(string? role) => role switch
        {
            Admin => "Administrador",
            Gerente => "Gerente",
            Vendedor => "Vendedor",
            Caixa => "Caixa",
            Estoquista => "Estoquista",
            _ => "—"
        };

        public static string Descricao(string? role) => role switch
        {
            Admin => "Acesso total: usuários, empresa, permissões e registros de auditoria.",
            Gerente => "Herda o operacional e acrescenta relatórios, dashboards e análise preditiva.",
            Vendedor => "Operação diária de vendas e consultas operacionais.",
            Caixa => "Operação diária de vendas e recebimentos.",
            Estoquista => "Operação diária de estoque e movimentações.",
            _ => string.Empty
        };
    }

    public class UsuarioListaViewModel
    {
        public string? Busca { get; set; }

        public string Situacao { get; set; } = SituacaoFiltro.Todos;

        public string Role { get; set; } = RolesUsuario.Todos;

        public IReadOnlyList<UsuarioLinhaViewModel> Usuarios { get; set; }
            = Array.Empty<UsuarioLinhaViewModel>();

        public PaginacaoViewModel Paginacao { get; set; } = PaginacaoViewModel.Criar(1, 0);
    }

    public class UsuarioLinhaViewModel
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string Role { get; set; } = string.Empty;

        public bool Ativo { get; set; }

        public DateTime DataCadastro { get; set; }

        public int QuantidadeVinculos { get; set; }

        public bool EhUsuarioAtual { get; set; }

        public string RoleRotulo => RolesUsuario.Rotulo(Role);

        public bool PodeExcluir => !Ativo && QuantidadeVinculos == 0 && !EhUsuarioAtual;
    }
}