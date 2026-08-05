using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using TCC_SistemaEmpresa.Validation;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    /// <summary>
    /// Formulário de criação, edição e visualização de funcionário.
    /// </summary>
    public class FuncionarioFormViewModel
    {
        /// <summary>
        /// Zero enquanto o registro não existe — a coluna é IDENTITY, quem gera é o banco.
        /// Exibição apenas: a tela mostra o valor, nunca o coleta.
        /// </summary>
        /// <remarks>
        /// <see cref="BindNeverAttribute"/> impede que o model binder preencha esta
        /// propriedade a partir da requisição. Sem ele, um POST forjado com "Id=99"
        /// sobrescreveria o valor — o binder lê o corpo do formulário antes da rota.
        /// Quem manda na identidade do registro é o parâmetro de rota da action.
        /// </remarks>
        [BindNever]
        public int Id { get; set; }

        /// <summary>
        /// Na criação, o número que a coluna IDENTITY deve usar no próximo INSERT.
        /// </summary>
        /// <remarks>
        /// É uma <b>previsão</b>, não uma reserva. O valor definitivo só existe quando
        /// o banco executa o INSERT: se outro usuário gravar antes, ou se um INSERT
        /// falhar (IDENTITY não devolve o número consumido), o registro nasce com um
        /// id maior que o exibido aqui. Serve para orientação na tela, nunca como
        /// chave para gravar ou referenciar.
        /// <c>null</c> quando não foi possível consultar — a tela cai no texto padrão.
        /// </remarks>
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

        // Range a partir de 1: o valor 0 é o item "Selecione o Cargo..." do dropdown,
        // que o [Required] sozinho não rejeitaria por ser um int não-nulo.
        [Range(1, int.MaxValue, ErrorMessage = "Selecione o cargo.")]
        [Display(Name = "Cargo")]
        public int CargoId { get; set; }

        /// <summary>Cargos ativos da empresa do usuário logado.</summary>
        public IEnumerable<SelectListItem> Cargos { get; set; } = Enumerable.Empty<SelectListItem>();

        /// <summary>Quando verdadeiro, a mesma tela é renderizada sem permitir edição.</summary>
        public bool SomenteLeitura { get; set; }

        public bool EhEdicao => Id > 0;
    }
}
