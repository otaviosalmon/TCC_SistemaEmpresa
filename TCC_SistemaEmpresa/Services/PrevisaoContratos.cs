using System.Text.Json.Serialization;
namespace TCC_SistemaEmpresa.Services
{
    public class PontoSerieDto
    {
        [JsonPropertyName("ano")]
        public int Ano { get; set; }

        [JsonPropertyName("mes")]
        public int Mes { get; set; }

        [JsonPropertyName("valor")] //perda de precisao aceitavel por n voltar ao banco e so para visualização grafica
        public decimal Valor { get; set; } 
    }

    public class SerieHistoricaDto
    {
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("pontos")]
        public List<PontoSerieDto> Pontos { get; set; } = new();
    }
    
    public class RequisicaoPrevisaoDto
    {
        [JsonPropertyName("meses_previsao")]
        public int MesesPrevisao { get; set; }

        [JsonPropertyName("series")]
        public List<SerieHistoricaDto> Series { get; set; } = new();
    }

    public class SeriePrevistaDto
    {
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("suficiente")]
        public bool Suficiente { get; set; }

        [JsonPropertyName("coeficiente_angular")]
        public decimal CoeficienteAngular { get; set; }

        [JsonPropertyName("intercepto")]
        public decimal Intercepto { get; set; }

        [JsonPropertyName("r2")]
        public double R2 { get; set; }

        [JsonPropertyName("pontos")]
        public List<PontoSerieDto> Pontos { get; set; } = new();
    }

    public class RespostaPrevisaoDto
    {
        [JsonPropertyName("series")]
        public List<SeriePrevistaDto> Series { get; set; } = new();
    }
}
