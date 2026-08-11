namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class ProdutoListaViewModel
    {
        public string? Busca { get; set; }

        public string Situacao { get; set; } = SituacaoFiltro.Todos;

        public IReadOnlyList<ProdutoLinhaViewModel> Produtos { get; set; }
            = Array.Empty<ProdutoLinhaViewModel>();
    }

    public class ProdutoLinhaViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public decimal PrecoVenda { get; set; }

        public int QuantidadeAtual { get; set; }

        public int? EstoqueMinimo { get; set; }

        public bool Ativo { get; set; }

        public int QuantidadeItensVenda { get; set; }

        public int QuantidadeMovimentacoes { get; set; }

        public bool PodeExcluir =>
            !Ativo && QuantidadeItensVenda == 0 && QuantidadeMovimentacoes == 0;

        public bool EstoqueBaixo =>
            Ativo && EstoqueMinimo.HasValue && QuantidadeAtual < EstoqueMinimo.Value;
    }
}
