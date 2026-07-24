namespace TCC_SistemaEmpresa.Models
{
    public class Venda
    {
        public int Id { get; set; }
        public int FuncionarioId { get; set; }
        public int EmpresaId { get; set; }
        public int? ClienteId { get; set; }
        public int FormaPagamentoId { get; set; }
        public DateTime DataVenda { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal Desconto { get; set; }
        public decimal ValorFinal { get; set; }
        public string? Observacao { get; set; }
    }
}
