using System.ComponentModel.DataAnnotations;

namespace TCC_SistemaEmpresa.Validation
{
    /// <summary>
    /// Valida CPF: 11 dígitos e dígitos verificadores corretos.
    /// </summary>
    /// <remarks>
    /// O banco (CHK_Funcionario_CPF) garante apenas o formato — 11 caracteres numéricos.
    /// A conferência do dígito verificador é responsabilidade da aplicação.
    /// A máscara digitada pelo usuário é ignorada aqui e removida antes de persistir:
    /// a coluna guarda somente dígitos.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class CpfAttribute : ValidationAttribute
    {
        public CpfAttribute()
            : base("O CPF informado é inválido.")
        {
        }

        public override bool IsValid(object? value)
        {
            var texto = value as string;

            // Campo vazio é assunto do [Required]; aqui não é erro.
            if (string.IsNullOrWhiteSpace(texto))
                return true;

            return EhValido(texto);
        }

        /// <summary>
        /// Remove tudo que não for dígito. É o formato gravado na coluna <c>cpf</c>.
        /// </summary>
        public static string ApenasDigitos(string? texto)
        {
            if (string.IsNullOrEmpty(texto))
                return string.Empty;

            return new string(texto.Where(char.IsDigit).ToArray());
        }

        public static bool EhValido(string? texto)
        {
            var cpf = ApenasDigitos(texto);

            if (cpf.Length != 11)
                return false;

            // Sequências como 111.111.111-11 passam no cálculo dos dígitos,
            // mas não são CPFs válidos.
            if (cpf.All(digito => digito == cpf[0]))
                return false;

            var numeros = cpf.Select(caractere => caractere - '0').ToArray();

            // Primeiro dígito verificador: pesos 10..2 sobre os 9 primeiros números.
            if (numeros[9] != CalcularDigito(numeros, 9))
                return false;

            // Segundo: pesos 11..2 sobre os 10 primeiros (já incluindo o primeiro DV).
            return numeros[10] == CalcularDigito(numeros, 10);
        }

        private static int CalcularDigito(int[] numeros, int quantidade)
        {
            var soma = 0;
            var peso = quantidade + 1;

            for (var indice = 0; indice < quantidade; indice++)
                soma += numeros[indice] * (peso - indice);

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }
}
