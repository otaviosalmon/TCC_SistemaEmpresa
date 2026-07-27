namespace TCC_SistemaEmpresa.Models
{
    public class Cargo
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal? SalarioBase { get; set; }
        public decimal? PerComissaoBase { get; set; }
        public bool Ativo { get; set; }
        public Empresa Empresa { get; set; }

    }
}
