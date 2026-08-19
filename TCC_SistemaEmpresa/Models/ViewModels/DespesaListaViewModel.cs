namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public static class RecorrenciaFiltro
    {
        public const string Todas = "todas";
        public const string Fixas = "fixas";
        public const string Eventuais = "eventuais";
    }

    public class DespesaListaViewModel
    {
        public string? Busca { get; set; }

        public string Recorrencia { get; set; } = RecorrenciaFiltro.Todas;

        public IReadOnlyList<DespesaLinhaViewModel> Despesas { get; set; }
            = Array.Empty<DespesaLinhaViewModel>();

        public decimal Total => Despesas.Sum(despesa => despesa.Valor);
    }

    public class DespesaLinhaViewModel
    {
        public int Id { get; set; }

        public string Categoria { get; set; } = string.Empty;

        public decimal Valor { get; set; }

        public DateTime DataDespesa { get; set; }

        public bool Fixa { get; set; }

        public string? Descricao { get; set; }
    }
}
