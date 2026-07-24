namespace TCC_SistemaEmpresa.Models
{
    public class Funcionario
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int CargoId { get; set; }
        public int? UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string? Telefone { get; set; }
        public string? Endereco { get; set; }
        public decimal? Salario { get; set; }
        public decimal? PerComissao { get; set; }
        public DateTime DataAdmissao { get; set; }
        public bool Ativo { get; set; }

    }
}
