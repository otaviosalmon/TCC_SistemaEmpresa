using System.ComponentModel.DataAnnotations.Schema;
namespace TCC_SistemaEmpresa.Models

{
    public class ItemVenda
    {
        // PK da Tb_Item_Venda (id INT IDENTITY). Sem esta propriedade o EF Core não
        // encontra chave por convenção e a validação do modelo derruba QUALQUER uso
        // do AppDbContext — inclusive o login, que nem consulta esta tabela.
        public int Id { get; set; }
        public int VendaId { get; set; }
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal? PrecoCusto { get; set; }
        public decimal Subtotal { get; set; }
        public Venda Venda { get; set; }
        public Produto Produto { get; set; }

    }
}
