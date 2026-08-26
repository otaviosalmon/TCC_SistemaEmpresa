namespace TCC_SistemaEmpresa.Models.ViewModels
{

    public class FuncionarioListaViewModel
    {
        public string? Busca { get; set; }

        public string Situacao { get; set; } = SituacaoFiltro.Todos;

        public IReadOnlyList<FuncionarioLinhaViewModel> Funcionarios { get; set; }
            = Array.Empty<FuncionarioLinhaViewModel>();
    }

    public static class SituacaoFiltro
    {
        public const string Todos = "todos";
        public const string Ativos = "ativos";
        public const string Inativos = "inativos";
    }


    public class FuncionarioLinhaViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Cargo { get; set; } = string.Empty;

        public decimal? Salario { get; set; }

        public bool Ativo { get; set; }


        public int QuantidadeVendas { get; set; }

        public bool PodeExcluir => !Ativo && QuantidadeVendas == 0;
    }
}
