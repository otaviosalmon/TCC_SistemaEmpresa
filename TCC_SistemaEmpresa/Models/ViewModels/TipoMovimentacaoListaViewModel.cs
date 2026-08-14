namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public static class NaturezaMovimentacao
    {
        public const string Entrada = "ENTRADA";
        public const string Saida = "SAIDA";

        public static bool EhValida(string? natureza) =>
            natureza == Entrada || natureza == Saida;

        public static string Rotulo(string? natureza) => natureza switch
        {
            Entrada => "Entrada",
            Saida => "Saída",
            _ => string.Empty
        };
    }

    public class TipoMovimentacaoListaViewModel
    {
        public string? Busca { get; set; }

        public string Situacao { get; set; } = SituacaoFiltro.Todos;

        public IReadOnlyList<TipoMovimentacaoLinhaViewModel> Tipos { get; set; }
            = Array.Empty<TipoMovimentacaoLinhaViewModel>();
    }

    public class TipoMovimentacaoLinhaViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Natureza { get; set; } = string.Empty;

        public bool Ativo { get; set; }

        public int QuantidadeMovimentacoes { get; set; }

        public string NaturezaRotulo => NaturezaMovimentacao.Rotulo(Natureza);

        public bool PodeExcluir => !Ativo && QuantidadeMovimentacoes == 0;
    }
}
