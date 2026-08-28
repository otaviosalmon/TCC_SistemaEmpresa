namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class FormaPagamentoListaViewModel
    {
        public string? Busca { get; set; }

        public string Situacao { get; set; } = SituacaoFiltro.Todos;

        public IReadOnlyList<FormaPagamentoLinhaViewModel> Formas { get; set; }
            = Array.Empty<FormaPagamentoLinhaViewModel>();
    }

    public class FormaPagamentoLinhaViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public bool Ativo { get; set; }

        public int QuantidadeVendas { get; set; }

        public bool PodeExcluir => !Ativo && QuantidadeVendas == 0;
    }
}
