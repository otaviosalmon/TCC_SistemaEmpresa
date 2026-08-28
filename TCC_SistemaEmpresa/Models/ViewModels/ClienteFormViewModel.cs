using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TCC_SistemaEmpresa.Validation;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class ClienteFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [BindNever]
        public DateTime? DataCadastro { get; set; }

        [BindNever]
        public int QuantidadeVendas { get; set; }

        [Required(ErrorMessage = "Informe o nome do cliente.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        [Display(Name = "Nome Cliente")]
        public string Nome { get; set; } = string.Empty;

        [Cpf]
        [Display(Name = "CPF")]
        public string? Cpf { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres.")]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [StringLength(255, ErrorMessage = "O endereço deve ter no máximo 255 caracteres.")]
        [Display(Name = "Endereço")]
        public string? Endereco { get; set; }

        public bool SomenteLeitura { get; set; }

        public bool EhEdicao => Id > 0;
    }
}
