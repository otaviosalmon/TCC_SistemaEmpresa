using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class CargoFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [BindNever]
        public int QuantidadeFuncionarios { get; set; }

        [BindNever]
        public int QuantidadeFuncionariosAtivos { get; set; }

        [Required(ErrorMessage = "Informe o nome do cargo.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Range(0, 99999999.99, ErrorMessage = "O salário deve estar entre 0 e 99.999.999,99.")]
        [Display(Name = "Salário Base")]
        public decimal? SalarioBase { get; set; }

        [Range(0, 100, ErrorMessage = "A comissão deve estar entre 0 e 100.")]
        [Display(Name = "Comissão Base")]
        public decimal? PerComissaoBase { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        public bool SomenteLeitura { get; set; }

        public bool EhEdicao => Id > 0;
    }
}