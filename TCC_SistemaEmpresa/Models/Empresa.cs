namespace TCC_SistemaEmpresa.Models
{
    public class Empresa
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Cnpj { get; set; }
        public string? Email { get; set; }
        public string? Cidade { get; set; }
        public string? Endereco { get; set; }
        public string? Estado { get; set; }
        public string? Cep { get; set; }
        public string? Telefone { get; set; }
        public bool Ativo { get; set; }



    }
}
