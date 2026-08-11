using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class ProdutoFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [BindNever]
        public DateTime? DataCadastro { get; set; }

        [Required(ErrorMessage = "Informe o nome do produto.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        [Display(Name = "Nome Produto")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Required(ErrorMessage = "Informe o preço de custo.")]
        [Range(0, 99999999.99, ErrorMessage = "O preço de custo deve estar entre 0 e 99.999.999,99.")]
        [Display(Name = "Preço Custo")]
        public decimal? PrecoCusto { get; set; }

        [Required(ErrorMessage = "Informe o preço de venda.")]
        [Range(0, 99999999.99, ErrorMessage = "O preço de venda deve estar entre 0 e 99.999.999,99.")]
        [Display(Name = "Preço Venda")]
        public decimal? PrecoVenda { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo.")]
        [Display(Name = "Estoque")]
        public int? QuantidadeAtual { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "O estoque mínimo não pode ser negativo.")]
        [Display(Name = "Estoque Min")]
        public int? EstoqueMinimo { get; set; }

        [StringLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione o tipo de produto.")]
        [Display(Name = "Tipo Produto")]
        public int CategoriaProdutoId { get; set; }

        public IEnumerable<SelectListItem> Categorias { get; set; } = Enumerable.Empty<SelectListItem>();

        public bool SomenteLeitura { get; set; }

        public bool EhEdicao => Id > 0;

        public bool TemCategorias => Categorias.Any();
    }
}
