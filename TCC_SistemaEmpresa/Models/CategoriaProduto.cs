namespace TCC_SistemaEmpresa.Models
{
    public class CategoriaProduto
    {
        public int EmpresaId { get; set; }
        public int Id { get; set; }
        public string Nome { get; set; }
        public string? Descricao { get; set; }
        public Empresa Empresa { get; set; }

    }
}
