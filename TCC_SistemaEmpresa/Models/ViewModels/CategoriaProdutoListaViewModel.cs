namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class CategoriaProdutoListaViewModel
    {
        public string? Busca { get; set; }

        public string Situacao { get; set; } = SituacaoFiltro.Todos;

        public IReadOnlyList<CategoriaProdutoLinhaViewModel> Categorias { get; set; }
            = Array.Empty<CategoriaProdutoLinhaViewModel>();

        public PaginacaoViewModel Paginacao { get; set; } = PaginacaoViewModel.Criar(1, 0);
    }

    public class CategoriaProdutoLinhaViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public int QuantidadeProdutos { get; set; }

        public bool Ativo { get; set; }

        public bool PodeExcluir => !Ativo && QuantidadeProdutos == 0;
    }
}
