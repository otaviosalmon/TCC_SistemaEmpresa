using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class DespesaFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione a categoria da despesa.")]
        [Display(Name = "Categoria Despesa")]
        public int CategoriaDespesaId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Data Despesa")]
        public DateTime DataDespesa { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Informe se a despesa é fixa.")]
        [Display(Name = "Fixa")]
        public bool? Fixa { get; set; }

        [Required(ErrorMessage = "Informe o valor da despesa.")]
        [Range(0.01, 99999999.99, ErrorMessage = "O valor deve estar entre 0,01 e 99.999.999,99.")]
        [Display(Name = "Valor Despesa")]
        public decimal? Valor { get; set; }

        [StringLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        public IEnumerable<SelectListItem> Categorias { get; set; } = Enumerable.Empty<SelectListItem>();

        public bool SomenteLeitura { get; set; }

        public bool EhEdicao => Id > 0;

        public bool TemCategorias => Categorias.Any();
    }
}
