using System.Globalization;

namespace TCC_SistemaEmpresa.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; }
        public string? Cpf { get; set; }
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? Endereco { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public bool Ativo { get; set; }
        public Empresa Empresa { get; set; }


    }
}
