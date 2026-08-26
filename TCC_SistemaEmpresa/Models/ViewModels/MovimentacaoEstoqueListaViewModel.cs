namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public static class NaturezaFiltro
    {
        public const string Todas = "todas";
        public const string Entradas = "entradas";
        public const string Saidas = "saidas";
    }

    public class MovimentacaoEstoqueListaViewModel
    {
        public string? Busca { get; set; }

        public string Natureza { get; set; } = NaturezaFiltro.Todas;

        public int ProdutoId { get; set; }

        public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Produtos { get; set; }
            = Enumerable.Empty<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

        public IReadOnlyList<MovimentacaoEstoqueLinhaViewModel> Movimentacoes { get; set; }
            = Array.Empty<MovimentacaoEstoqueLinhaViewModel>();

        public int TotalEntradas => Movimentacoes
            .Where(movimentacao => movimentacao.Natureza == NaturezaMovimentacao.Entrada)
            .Sum(movimentacao => movimentacao.Quantidade);

        public int TotalSaidas => Movimentacoes
            .Where(movimentacao => movimentacao.Natureza == NaturezaMovimentacao.Saida)
            .Sum(movimentacao => movimentacao.Quantidade);
    }

    public class MovimentacaoEstoqueLinhaViewModel
    {
        public int Id { get; set; }

        public string Produto { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public string Natureza { get; set; } = string.Empty;

        public int Quantidade { get; set; }

        public int? QuantidadeAntes { get; set; }

        public int? QuantidadeDepois { get; set; }

        public DateTime DataMovimentacao { get; set; }

        public int? VendaId { get; set; }

        public string NaturezaRotulo => NaturezaMovimentacao.Rotulo(Natureza);

        public bool OriginadaDeVenda => VendaId.HasValue;

        public bool PodeExcluir => !OriginadaDeVenda;
    }
}
