namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class VendaListaViewModel
    {
        public string? Busca { get; set; }
        public DateTime? DataInicial { get; set; }
        public DateTime? DataFinal { get; set; }
        public string Filtro { get; set; } = FiltroVenda.Todas;
        public IReadOnlyList<VendaLinhaViewModel> Vendas { get; set; }
            = Array.Empty<VendaLinhaViewModel>();
    }
    public static class FiltroVenda
    {
        public const string Todas = "todas";
        public const string Concluidas = "concluidas";
        public const string Canceladas = "canceladas";
    }
    public static class SituacaoVenda
    {
        public const string Concluida = "CONCLUIDA";
        public const string Cancelada = "CANCELADA";
    }
    public class VendaLinhaViewModel
    {
        public int Id { get; set; }
        public DateTime DataVenda { get; set; }
        public string Funcionario { get; set; } = string.Empty;
        public string Cliente { get; set; } = "Não identificado";
        public string FormaPagamento { get; set; } = string.Empty;
        public int QuantidadeItens { get; set; }
        public decimal ValorFinal { get; set; }
        public string SituacaoVenda { get; set; } = ViewModels.SituacaoVenda.Concluida;

    }
}
