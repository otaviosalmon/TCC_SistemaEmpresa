namespace TCC_SistemaEmpresa.Models
{
    public class TipoMovimentacao
    {
        public int EmpresaId { get; set; }
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Natureza { get; set; }
        public string? Descricao { get; set; }
        public bool Ativo { get; set; }
        public Empresa Empresa { get; set; }

    }
}
