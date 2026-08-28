using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class FormaPagamentoFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [BindNever]
        public int QuantidadeVendas { get; set; }

        [Required(ErrorMessage = "Informe o nome da forma de pagamento.")]
        [StringLength(50, ErrorMessage = "O nome deve ter no máximo 50 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "A descrição deve ter no máximo 150 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        public bool SomenteLeitura { get; set; }

        public bool EhEdicao => Id > 0;
    }
}
