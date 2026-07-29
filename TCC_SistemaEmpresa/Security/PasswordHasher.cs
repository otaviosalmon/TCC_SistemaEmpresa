using System.Security.Cryptography;
using System.Text;

namespace TCC_SistemaEmpresa.Security
{
    /// <summary>
    /// Geração e verificação do hash de senha do sistema.
    ///
    /// O hash é derivado do par (usuário, senha): o username funciona como salt.
    /// Consequências dessa escolha:
    ///   • Determinístico — o mesmo par sempre gera o mesmo hash. É o que permite
    ///     gerar um INSERT de teste direto no banco (ver database/insert_usuario_teste.sql).
    ///   • Dois usuários com a senha '123' têm hashes diferentes, porque o salt difere.
    ///   • Trocar o username de um usuário INVALIDA a senha dele — é preciso regerar o hash.
    ///
    /// O algoritmo é PBKDF2-HMAC-SHA256, que é lento de propósito: encarece ataque
    /// de força bruta. Nunca trocar por SHA256/MD5 "puro" (instantâneos de quebrar).
    ///
    /// Formato persistido em Tb_Usuario.password_hash (VARCHAR(255)):
    ///     PBKDF2-SHA256$&lt;iteracoes&gt;$&lt;hash em base64&gt;
    /// As iterações ficam gravadas na própria string para que o custo possa ser
    /// aumentado no futuro sem invalidar os hashes já existentes.
    ///
    /// COMPATIBILIDADE: o banco de desenvolvimento já tinha usuários gravados em um
    /// formato anterior, com salt aleatório embutido na string:
    ///     PBKDF2$sha256$&lt;iteracoes&gt;$&lt;salt base64&gt;$&lt;hash base64&gt;
    /// <see cref="Verificar"/> aceita os dois formatos, para que esses usuários
    /// continuem conseguindo logar. Hashes NOVOS saem sempre no formato atual.
    /// </summary>
    public static class PasswordHasher
    {
        private const string Algoritmo = "PBKDF2-SHA256";

        /// <summary>Custo do KDF. Recomendação OWASP para PBKDF2-HMAC-SHA256.</summary>
        private const int Iteracoes = 210_000;

        private const int TamanhoHashBytes = 32;

        /// <summary>
        /// Prefixo de domínio no salt. Impede que um hash gerado aqui colida com o de
        /// outro sistema que também use "username como salt".
        /// </summary>
        private const string ContextoSalt = "LOSolutions.v1|";

        private static readonly HashAlgorithmName Hmac = HashAlgorithmName.SHA256;

        /// <summary>
        /// Gera o hash a ser gravado em Tb_Usuario.password_hash.
        /// </summary>
        public static string GerarHash(string username, string senha)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("O username é obrigatório para gerar o hash.", nameof(username));

            ArgumentNullException.ThrowIfNull(senha);

            var hash = Derivar(username, senha, Iteracoes);
            return $"{Algoritmo}${Iteracoes}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Confere a senha informada no login contra o hash gravado no banco.
        /// Aceita o formato atual e o formato legado (ver observação na classe).
        /// Retorna false para qualquer entrada inválida ou hash malformado — nunca lança.
        /// </summary>
        public static bool Verificar(string username, string senha, string? hashArmazenado)
        {
            if (senha is null || string.IsNullOrWhiteSpace(hashArmazenado))
                return false;

            // O número de segmentos distingue os formatos:
            //   3 → PBKDF2-SHA256$iteracoes$hash                (atual, salt = username)
            //   5 → PBKDF2$sha256$iteracoes$salt$hash           (legado, salt aleatório)
            var partes = hashArmazenado.Split('$');

            return partes.Length switch
            {
                3 => VerificarFormatoAtual(username, senha, partes),
                5 => VerificarFormatoLegado(senha, partes),
                _ => false
            };
        }

        private static bool VerificarFormatoAtual(string username, string senha, string[] partes)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            if (partes[0] != Algoritmo)
                return false;

            if (!int.TryParse(partes[1], out var iteracoes) || iteracoes <= 0)
                return false;

            if (!TentarBase64(partes[2], out var esperado) || esperado.Length != TamanhoHashBytes)
                return false;

            var calculado = Derivar(username, senha, iteracoes);

            // Comparação em tempo fixo: uma comparação comum (==) vaza, pelo tempo de
            // resposta, quantos bytes iniciais do hash estavam corretos.
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }

        /// <summary>
        /// Formato antigo: o salt é aleatório e vem gravado dentro da própria string,
        /// então o username não participa da verificação.
        /// </summary>
        private static bool VerificarFormatoLegado(string senha, string[] partes)
        {
            if (!partes[0].Equals("PBKDF2", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!partes[1].Equals("sha256", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!int.TryParse(partes[2], out var iteracoes) || iteracoes <= 0)
                return false;

            if (!TentarBase64(partes[3], out var salt) || salt.Length == 0)
                return false;

            if (!TentarBase64(partes[4], out var esperado) || esperado.Length == 0)
                return false;

            var calculado = Rfc2898DeriveBytes.Pbkdf2(
                password: Encoding.UTF8.GetBytes(senha),
                salt: salt,
                iterations: iteracoes,
                hashAlgorithm: Hmac,
                outputLength: esperado.Length);

            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }

        private static bool TentarBase64(string valor, out byte[] bytes)
        {
            try
            {
                bytes = Convert.FromBase64String(valor);
                return true;
            }
            catch (FormatException)
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }

        private static byte[] Derivar(string username, string senha, int iteracoes) =>
            Rfc2898DeriveBytes.Pbkdf2(
                password: Encoding.UTF8.GetBytes(senha),
                salt: DerivarSalt(username),
                iterations: iteracoes,
                hashAlgorithm: Hmac,
                outputLength: TamanhoHashBytes);

        /// <summary>
        /// Converte o username em 32 bytes de salt.
        ///
        /// O username é normalizado (trim + minúsculas) porque o banco usa collation
        /// Latin1_General_CI_AI: 'Teste' e 'TESTE' são o MESMO usuário para o SQL Server.
        /// Sem normalizar, logar com outra caixa geraria um salt diferente e a senha
        /// correta seria recusada.
        /// </summary>
        private static byte[] DerivarSalt(string username)
        {
            var normalizado = username.Trim().ToLowerInvariant();
            return SHA256.HashData(Encoding.UTF8.GetBytes(ContextoSalt + normalizado));
        }
    }
}
