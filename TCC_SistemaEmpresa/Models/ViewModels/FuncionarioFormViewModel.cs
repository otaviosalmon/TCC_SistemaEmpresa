using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using TCC_SistemaEmpresa.Validation;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class FuncionarioFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [Required(ErrorMessage = "Informe o nome do funcionário.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        [Display(Name = "Nome Funcionário")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o CPF.")]
        [Cpf]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = string.Empty;

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Range(0, 99999999.99, ErrorMessage = "O salário deve estar entre 0 e 99.999.999,99.")]
        [Display(Name = "Salário")]
        public decimal? Salario { get; set; }

        [Range(0, 100, ErrorMessage = "A comissão deve estar entre 0 e 100.")]
        [Display(Name = "Comissão")]
        public decimal? PerComissao { get; set; }

        [Required(ErrorMessage = "Informe a data de admissão.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data de Admissão")]
        public DateTime DataAdmissao { get; set; } = DateTime.Today;


        [Range(1, int.MaxValue, ErrorMessage = "Selecione o cargo.")]
        [Display(Name = "Cargo")]
        public int CargoId { get; set; }

        public IEnumerable<SelectListItem> Cargos { get; set; } = Enumerable.Empty<SelectListItem>();

        public bool SomenteLeitura { get; set; }

        public bool EhEdicao => Id > 0;
    }
}
