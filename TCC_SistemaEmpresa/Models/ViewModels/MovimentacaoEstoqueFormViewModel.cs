using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class ProdutoOpcaoViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public int Saldo { get; set; }

        public bool Ativo { get; set; }

        public string Rotulo => Ativo ? Nome : $"{Nome} (inativo)";
    }

    public class TipoMovimentacaoOpcaoViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Natureza { get; set; } = string.Empty;

        public bool Ativo { get; set; }

        public string Rotulo => Ativo ? Nome : $"{Nome} (inativo)";
    }

    public class MovimentacaoEstoqueFormViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int? ProximoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione o produto movimentado.")]
        [Display(Name = "Produto")]
        public int ProdutoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione o tipo de movimentação.")]
        [Display(Name = "Tipo Movimentação")]
        public int TipoMovimentacaoEstoqueId { get; set; }

        [BindNever]
        public DateTime? DataMovimentacao { get; set; }

        [Required(ErrorMessage = "Informe a quantidade movimentada.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
        [Display(Name = "Quantidade")]
        public int? Quantidade { get; set; }

        [StringLength(255, ErrorMessage = "A observação deve ter no máximo 255 caracteres.")]
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        [BindNever]
        public int? QuantidadeAntes { get; set; }

        [BindNever]
        public int? QuantidadeDepois { get; set; }

        [BindNever]
        public string? Natureza { get; set; }

        [BindNever]
        public int? VendaId { get; set; }

        public IReadOnlyList<ProdutoOpcaoViewModel> Produtos { get; set; }
            = Array.Empty<ProdutoOpcaoViewModel>();

        public IReadOnlyList<TipoMovimentacaoOpcaoViewModel> Tipos { get; set; }
            = Array.Empty<TipoMovimentacaoOpcaoViewModel>();

        public bool SomenteLeitura { get; set; }

        public bool EhRegistroGravado => Id > 0;

        public bool TemProdutos => Produtos.Count > 0;

        public bool TemTipos => Tipos.Count > 0;

        public bool PodeSalvar => TemProdutos && TemTipos;

        public bool OriginadaDeVenda => VendaId.HasValue;

        public string NaturezaRotulo => NaturezaMovimentacao.Rotulo(Natureza);
    }
}
