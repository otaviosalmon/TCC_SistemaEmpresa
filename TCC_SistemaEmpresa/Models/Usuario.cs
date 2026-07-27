namespace TCC_SistemaEmpresa.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Username { get; set; }
        public string? Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public Empresa Empresa { get; set; }
    }
}
