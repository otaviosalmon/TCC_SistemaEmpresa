using System.ComponentModel.DataAnnotations.Schema;
namespace TCC_SistemaEmpresa.Models

{
    public class ItemVenda
    {
        public int VendaId { get; set; }
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public Venda Venda { get; set; }
        public Produto Produto { get; set; }

    }
}
