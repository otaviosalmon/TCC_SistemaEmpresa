using System.ComponentModel.DataAnnotations;

namespace TCC_SistemaEmpresa.Validation
{

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

            if (string.IsNullOrWhiteSpace(texto))
                return true;

            return EhValido(texto);
        }

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

            if (cpf.All(digito => digito == cpf[0]))
                return false;

            var numeros = cpf.Select(caractere => caractere - '0').ToArray();

            if (numeros[9] != CalcularDigito(numeros, 9))
                return false;

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
