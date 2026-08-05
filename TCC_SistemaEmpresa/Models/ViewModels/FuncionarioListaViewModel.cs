namespace TCC_SistemaEmpresa.Models.ViewModels
{
    /// <summary>
    /// Tela de gerenciamento (listagem) de funcionários, com busca e filtro.
    /// </summary>
    public class FuncionarioListaViewModel
    {
        /// <summary>Texto livre: casa com nome ou CPF.</summary>
        public string? Busca { get; set; }

        /// <summary>Um dos valores de <see cref="SituacaoFiltro"/>.</summary>
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

    /// <summary>Uma linha da tabela de funcionários.</summary>
    public class FuncionarioLinhaViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Cargo { get; set; } = string.Empty;

        /// <summary>
        /// Salário efetivo: o do funcionário quando preenchido, senão o padrão do cargo.
        /// Mesma precedência que a view Vw_Resumo_Vendas_Funcionario usa para a comissão.
        /// </summary>
        public decimal? Salario { get; set; }

        public bool Ativo { get; set; }

        /// <summary>
        /// Vendas ligadas a este funcionário. Enquanto for maior que zero, a exclusão
        /// definitiva é impossível — FK_Venda_Funcionario perderia a referência e o
        /// histórico de vendas ficaria órfão.
        /// </summary>
        public int QuantidadeVendas { get; set; }

        /// <summary>
        /// Só registro inativo e sem vínculo pode ser excluído de vez.
        /// </summary>
        public bool PodeExcluir => !Ativo && QuantidadeVendas == 0;
    }
}
