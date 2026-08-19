using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class CategoriaDespesaFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [BindNever]
        public int QuantidadeDespesas { get; set; }

        [Required(ErrorMessage = "Informe o nome da categoria de despesa.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        public bool SomenteLeitura { get; set; }

        public bool EhEdicao => Id > 0;
    }
}
