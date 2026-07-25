namespace TCC_SistemaEmpresa.Models
{
    public class Produto
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int CategoriaProdutoId { get; set; }
        public string Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal PrecoCusto { get; set; }
        public decimal PrecoVenda { get; set; }
        public int QuantidadeAtual { get; set; }
        public int? EstoqueMinimo { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public bool Ativo { get; set; }
        public Empresa Empresa { get; set; }
        public CategoriaProduto CategoriaProduto { get; set; }

    }
}
