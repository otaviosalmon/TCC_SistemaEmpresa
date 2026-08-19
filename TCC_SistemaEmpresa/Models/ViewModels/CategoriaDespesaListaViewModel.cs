namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class CategoriaDespesaListaViewModel
    {
        public string? Busca { get; set; }

        public string Situacao { get; set; } = SituacaoFiltro.Todos;

        public IReadOnlyList<CategoriaDespesaLinhaViewModel> Categorias { get; set; }
            = Array.Empty<CategoriaDespesaLinhaViewModel>();
    }

    public class CategoriaDespesaLinhaViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public bool Ativo { get; set; }

        public int QuantidadeDespesas { get; set; }

        public bool PodeExcluir => !Ativo && QuantidadeDespesas == 0;
    }
}
