using System.Globalization;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class DashboardViewModel
    {
        public DateTime PeriodoInicial {get; set; }
        public DateTime PeriodoFinal { get; set; }
        public decimal ReceitaBruta { get; set; }
        public decimal CustoProdutosVendidos { get; set; }
        public decimal ReceitaLiquida => ReceitaBruta - CustoProdutosVendidos;
        public decimal TotalDespesas { get; set; }
        public decimal LucroTotal => ReceitaLiquida - TotalDespesas;
        public int QuantidadeVendas { get; set; }

        public IReadOnlyList<ProdutoMaisVendidoViewModel> ProdutosMaisVendidos { get; set; }
                = Array.Empty<ProdutoMaisVendidoViewModel>();

        public IReadOnlyList<FaturamentoMensalViewModel> EvolucaoFaturamento { get; set; }
                = Array.Empty<FaturamentoMensalViewModel>();

        public bool TemVendas => QuantidadeVendas > 0;
        public bool TemProdutosVendidos => ProdutosMaisVendidos.Count > 0;
        public string PeriodoDescricao => $"{PeriodoInicial:dd/MM/yyyy} a {PeriodoFinal:dd/MM/yyyy}";
    }

    public class ProdutoMaisVendidoViewModel
    {
        public string Produto { get; set; }
        public int Quantidade { get; set; }
        public decimal ValorVendido { get; set; }
    }

    public class FaturamentoMensalViewModel
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public decimal Total { get; set; }
        public string Rotulo => $"{Mes:00}/{Ano % 100:00}";
        public string RotuloCompleto =>
            new DateTime(Ano, Mes, 1).ToString("MMMM 'de' yyyy", new CultureInfo("pt-BR"));
    }
}
