using System.Net.Http.Json;
namespace TCC_SistemaEmpresa.Services
{
    public class PrevisaoService
    {
        private readonly HttpClient _http;
        private readonly ILogger<PrevisaoService> _logger;

        public PrevisaoService(HttpClient http, ILogger<PrevisaoService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<RespostaPrevisaoDto?> PreverAsync(
            RequisicaoPrevisaoDto requisicao, CancellationToken cancelamento = default)
        {
            try
            {
                var resposta = await _http.PostAsJsonAsync("previsao", requisicao, cancelamento);

                if (!resposta.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Serviço de previsão respondeu {StatusCode}.",
                        (int)resposta.StatusCode);
                    return null;
                }

                return await resposta.Content
                    .ReadFromJsonAsync<RespostaPrevisaoDto>(cancelamento);
            }
            catch (Exception excecao) when (
            excecao is  HttpRequestException || excecao is TaskCanceledException)
            {
                _logger.LogError(excecao, "Não foi possivel contatar o serviço de provisão {BaseAddress}." +
                    "Verifique se o uvicorn está rodando.", _http.BaseAddress);

                return null;
            }
        }
    }
}
