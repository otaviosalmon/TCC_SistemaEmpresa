namespace TCC_SistemaEmpresa.Models
{
    public class MovimentacaoEstoque
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int ProdutoId { get; set; }
        public int UsuarioId { get; set; }
        public int? VendaId { get; set; }
        public int TipoMovimentacaoEstoqueId { get; set; }
        public int Quantidade { get; set; }
        public int? QuantidadeAntes { get; set; }
        public int? QuantidadeDepois { get; set; }
        public DateTime DataMovimentacao { get; set; } = DateTime.Now;
        public string? Observacao { get; set; }
        public Empresa Empresa { get; set; }
        public Produto Produto { get; set; }
        public Usuario Usuario { get; set; }
        public Venda Venda { get; set; }

    }
}
