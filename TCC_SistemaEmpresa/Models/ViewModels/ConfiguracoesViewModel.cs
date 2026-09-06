using TCC_SistemaEmpresa.Services;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class ConfiguracoesViewModel
    {
        public string Tema { get; set; } = Aparencia.Tema.Padrao;

        public string Fonte { get; set; } = Aparencia.Fonte.Padrao;

        public string Densidade { get; set; } = Aparencia.Densidade.Padrao;

        public string Empresa { get; set; } = string.Empty;

        public string Usuario { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Perfil { get; set; } = string.Empty;

        public EmpresaResumoViewModel? DadosEmpresa { get; set; }

        public string PerfilDescricao => Perfil switch
        {
            "ADMIN" => "Administrador",
            "GERENTE" => "Gerente",
            "VENDEDOR" => "Vendedor",
            "CAIXA" => "Caixa",
            "ESTOQUISTA" => "Estoquista",
            _ => "Não informado"
        };
    }

    public class EmpresaResumoViewModel
    {
        public string Nome { get; set; } = string.Empty;

        public string Cnpj { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Telefone { get; set; }

        public string? Endereco { get; set; }

        public string? Cidade { get; set; }

        public string? Estado { get; set; }

        public string? Cep { get; set; }

        public bool Ativo { get; set; }

        public string CnpjFormatado => Cnpj.Length == 14
            ? $"{Cnpj[..2]}.{Cnpj[2..5]}.{Cnpj[5..8]}/{Cnpj[8..12]}-{Cnpj[12..]}"
            : Cnpj;

        public string CepFormatado => Cep is { Length: 8 }
            ? $"{Cep[..5]}-{Cep[5..]}"
            : Cep ?? string.Empty;

        public string Municipio => string.IsNullOrWhiteSpace(Cidade)
            ? Estado ?? string.Empty
            : string.IsNullOrWhiteSpace(Estado) ? Cidade : $"{Cidade} / {Estado}";

        public string Situacao => Ativo ? "Ativa" : "Inativa";
    }
}
