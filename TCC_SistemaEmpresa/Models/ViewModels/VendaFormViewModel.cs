using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TCC_SistemaEmpresa.Models.ViewModels

{
    public class VendaFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione o Funcionário Responsavel pela venda.")]
        [Display(Name = "Funcionário")]
        public int FuncionarioId { get; set; }

        [Display(Name = "Cliente")]
        public int? ClienteId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione a forma de pagamento.")]
        [Display(Name = "Forma de Pagamento")]
        public int FormaPagamentoId { get; set; }

        [BindNever]
        [Display(Name = "Data Venda")]
        public DateTime? DataVenda { get; set; }

        [Range(0, 99999999.99, ErrorMessage = "O desconto não pode ser negativo.")]
        [Display(Name = "Desconto")]
        public decimal Desconto { get; set; }

        [StringLength(255, ErrorMessage = "A observação pode ter no máximo 255 caracteres")]
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        public List<ItemVendaFormViewModel> Itens { get; set; } = new() { new ItemVendaFormViewModel() };

        [BindNever] public decimal? ValorTotal { get; set; }
        [BindNever] public decimal? ValorFinal { get; set; }

        [BindNever]
        public string SituacaoVenda { get; set; } = ViewModels.SituacaoVenda.Concluida;

        public IReadOnlyList<ItemVendaLinhaViewModel> ItensDetalhe { get; set; }
            = Array.Empty<ItemVendaLinhaViewModel>();

        [BindNever] public string? FuncionarioNome { get; set; }
        [BindNever] public string? ClienteNome { get; set; }
        [BindNever] public string? FormaPagamentoNome { get; set; }

        public IEnumerable<SelectListItem> Funcionarios { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Clientes { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> FormaPagamento { get; set; } = Enumerable.Empty<SelectListItem>();

        public IReadOnlyList<ProdutoOpcaoViewModel> ProdutosDisponiveis { get; set; }
            = Array.Empty<ProdutoOpcaoViewModel>();


        public bool SomenteLeitura { get; set; }
        public bool TemFuncionarios => Funcionarios.Any();
        public bool TemProdutos => ProdutosDisponiveis.Any();
        public bool TemFormasPagamento => FormaPagamento.Any();

        public bool PodeVender => TemFuncionarios && TemFormasPagamento && TemProdutos;
        public bool PodeCancelar => SomenteLeitura && SituacaoVenda == ViewModels.SituacaoVenda.Concluida;

        public class ItemVendaFormViewModel
        {
            [Range(1, int.MaxValue, ErrorMessage = "Selecione um produto.")]
            public int ProdutoId { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser de pelo menos 1.")]
            public int Quantidade { get; set; }

        }

        public class ItemVendaLinhaViewModel
        {
            public string Produto { get; set; }
            public int Quantidade { get; set; }
            public decimal PrecoUnitario { get; set; }
            public decimal Subtotal { get; set; }

        }
        public class ProdutoOpcaoViewModel
        {
            public int Id { get; set; }
            public string Nome { get; set; } = string.Empty;
            public decimal PrecoVenda { get; set; }
            public int QuantidadeAtual { get; set; }

        }

    }
}
