using System.Globalization;
namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class PrevisaoViewModel
    {
        public DateTime PeriodoInicial { get; set; }
        public DateTime PeriodoFinal { get; set; }
        public int MesesPrevisao { get; set; }

        public IReadOnlyList<PontoMensalViewModel> FaturamentoHistorico { get; set; }
            = Array.Empty<PontoMensalViewModel>();

        public IReadOnlyList<PontoMensalViewModel> DespesasHistorico { get; set; }
            = Array.Empty<PontoMensalViewModel>();

        public SerieProjetadaViewModel? FaturamentoPrevisto { get; set; }
        public SerieProjetadaViewModel? DespesasPrevistas { get; set; }

        public bool ServicoIndisponivel { get; set; }
        public int MesesDeHistorico => FaturamentoHistorico.Count;
        public bool TemHistoricoSuficiente => MesesDeHistorico >= 3;

        public bool TemPrevisao =>
            !ServicoIndisponivel &&
            FaturamentoPrevisto is { Suficiente: true } &&
            DespesasPrevistas is { Suficiente: true };

        public decimal FaturamentoPrevistoTotal => FaturamentoPrevisto?.Pontos.Sum(p => p.Total) ?? 0m;
        public decimal DespesasPrevistasTotal => DespesasPrevistas?.Pontos.Sum(p => p.Total) ?? 0m;
        public decimal ResultadoPrevisto => FaturamentoPrevistoTotal - DespesasPrevistasTotal;

        public string PeriodoDescricao => $"{PeriodoInicial:dd/MM/yyyy} a {PeriodoFinal:dd/MM/yyyy}";

        public string HorizonteDescricao
        {
            get
            {
                if (FaturamentoPrevisto is null || FaturamentoPrevisto.Pontos.Count == 0)
                    return "-";

                var primeiro = FaturamentoPrevisto.Pontos[0];
                var ultimo = FaturamentoPrevisto.Pontos[^1];

                return $"{primeiro.Mes:00}/{primeiro.Ano} a {ultimo.Mes:00}/{ultimo.Ano}";
            }
        }
    }

    public class PontoMensalViewModel
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public decimal Total { get; set; }

        public string Rotulo => $"{Mes:00}/{Ano % 100:00}";

        public string RotuloCompleto =>
            new DateTime(Ano, Mes, 1).ToString("MMMM 'de' yyyy", new CultureInfo("pt-BR"));
    }

    public class SerieProjetadaViewModel
    {
        public bool Suficiente { get; set; }
        public decimal CoeficienteAngular { get; set; }
        public double R2 { get; set; }

        public IReadOnlyList<PontoMensalViewModel> Pontos { get; set; }
            = Array.Empty<PontoMensalViewModel>();

        public string ConfiancaTexto => $"{R2 * 100:0.0}%";
        public string TendenciaTexto => CoeficienteAngular switch
        {
            > 0 => $"alta de {CoeficienteAngular:C} por mês",
            < 0 => $"queda de {Math.Abs(CoeficienteAngular):C} por mês",
            _ => "estável"
        };
    }
}
