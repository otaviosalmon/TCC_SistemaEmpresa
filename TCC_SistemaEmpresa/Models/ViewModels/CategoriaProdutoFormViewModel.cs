using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class CategoriaProdutoFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [BindNever]
        public int QuantidadeProdutos { get; set; }

        [Required(ErrorMessage = "Informe o nome do tipo de produto.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        public bool SomenteLeitura { get; set; }

        public bool EhEdicao => Id > 0;
    }
}
