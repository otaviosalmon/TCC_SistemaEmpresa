namespace TCC_SistemaEmpresa.Models
{
    public class Venda
    {
        public int Id { get; set; }
        public int FuncionarioId { get; set; }
        public int EmpresaId { get; set; }
        public int? ClienteId { get; set; }
        public int FormaPagamentoId { get; set; }
        public DateTime DataVenda { get; set; } = DateTime.Now;
        public decimal ValorTotal { get; set; }
        public decimal Desconto { get; set; }
        public decimal ValorFinal { get; set; }
        public string? Observacao { get; set; }
        public string SituacaoVenda { get; set; } = "CONCLUIDA";
        public Empresa Empresa { get; set; }
        public Funcionario Funcionario { get; set; }
        public Cliente Cliente { get; set; }
        public FormaPagamento FormaPagamento { get; set; }

    }
}
