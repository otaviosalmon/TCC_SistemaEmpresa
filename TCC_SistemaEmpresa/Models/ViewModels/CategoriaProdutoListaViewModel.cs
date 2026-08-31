namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class CategoriaProdutoListaViewModel
    {
        public string? Busca { get; set; }

        public IReadOnlyList<CategoriaProdutoLinhaViewModel> Categorias { get; set; }
            = Array.Empty<CategoriaProdutoLinhaViewModel>();
    }

    public class CategoriaProdutoLinhaViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public int QuantidadeProdutos { get; set; }

        public bool PodeExcluir => QuantidadeProdutos == 0;
    }
}
