using System.ComponentModel.DataAnnotations;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe o usuário.")]
        [StringLength(50, ErrorMessage = "O usuário deve ter no máximo 50 caracteres.")]
        [Display(Name = "Usuário")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;
    }
}
