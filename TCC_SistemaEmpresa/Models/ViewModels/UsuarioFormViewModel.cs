using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class UsuarioFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [Required(ErrorMessage = "Informe o nome de usuário.")]
        [StringLength(50, ErrorMessage = "O usuário deve ter no máximo 50 caracteres.")]
        [RegularExpression("^[A-Za-z0-9._-]+$",
            ErrorMessage = "Use apenas letras, números, ponto, traço ou sublinhado.")]
        [Display(Name = "Usuário")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o perfil de acesso.")]
        [Display(Name = "Perfil de Acesso")]
        public string Role { get; set; } = RolesUsuario.Vendedor;

        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string? Senha { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Senha), ErrorMessage = "A confirmação não confere com a senha.")]
        [Display(Name = "Confirmar Senha")]
        public string? ConfirmarSenha { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [BindNever]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        [BindNever]
        public bool SomenteLeitura { get; set; }

        [BindNever]
        public bool EhUsuarioAtual { get; set; }

        [BindNever]
        public string? FuncionarioVinculado { get; set; }

        [BindNever]
        public int QuantidadeVinculos { get; set; }

        public IEnumerable<SelectListItem> Roles { get; set; } = Enumerable.Empty<SelectListItem>();

        public bool EhEdicao => Id > 0;

        public bool SenhaObrigatoria => !EhEdicao;
    }
}