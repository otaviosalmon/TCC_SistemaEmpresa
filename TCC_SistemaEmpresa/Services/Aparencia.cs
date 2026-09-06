namespace TCC_SistemaEmpresa.Services
{
    public class PreferenciaAparencia
    {
        private readonly string[] _valores;

        public PreferenciaAparencia(string cookie, params string[] valores)
        {
            Cookie = cookie;
            _valores = valores;
        }

        public string Cookie { get; }

        public string Padrao => _valores[0];

        public string Normalizar(string? valor) =>
            valor is not null && _valores.Contains(valor) ? valor : Padrao;

        public string Atual(HttpRequest requisicao) =>
            Normalizar(requisicao.Cookies[Cookie]);

        public void Gravar(HttpResponse resposta, string? valor) =>
            resposta.Cookies.Append(Cookie, Normalizar(valor), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
    }

    public static class Aparencia
    {
        public const string TemaEscuro = "escuro";
        public const string TemaClaro = "claro";
        public const string TemaDispositivo = "sistema";

        public const string FonteNormal = "normal";
        public const string FonteGrande = "grande";
        public const string FonteMaior = "maior";

        public const string DensidadeConfortavel = "confortavel";
        public const string DensidadeCompacta = "compacta";

        public static readonly PreferenciaAparencia Tema =
            new("LOSolutions.Tema", TemaEscuro, TemaClaro, TemaDispositivo);

        public static readonly PreferenciaAparencia Fonte =
            new("LOSolutions.Fonte", FonteNormal, FonteGrande, FonteMaior);

        public static readonly PreferenciaAparencia Densidade =
            new("LOSolutions.Densidade", DensidadeConfortavel, DensidadeCompacta);
    }
}
