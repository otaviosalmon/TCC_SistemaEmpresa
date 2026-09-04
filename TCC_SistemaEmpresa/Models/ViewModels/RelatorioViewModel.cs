namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public static class TipoRelatorio
    {
        public const string Movimentacoes = "movimentacoes";
        public const string Vendas = "vendas";
        public const string Despesas = "despesas";

        public static string Normalizar(string? tipo) => tipo switch
        {
            Vendas => Vendas,
            Despesas => Despesas,
            _ => Movimentacoes
        };
    }

    public enum FormatoColuna
    {
        Texto,
        Inteiro,
        Moeda,
        Data,
        DataHora
    }

    public class RelatorioColunaViewModel
    {
        public string Titulo { get; init; } = string.Empty;

        public FormatoColuna Formato { get; init; } = FormatoColuna.Texto;

        public bool Numerica => Formato is FormatoColuna.Inteiro or FormatoColuna.Moeda;
    }

    public class ProdutoFiltroViewModel
    {
        public int Id { get; init; }

        public string Nome { get; init; } = string.Empty;
    }

    public class RelatorioTotalViewModel
    {
        public string Rotulo { get; init; } = string.Empty;

        public object? Valor { get; init; }

        public FormatoColuna Formato { get; init; } = FormatoColuna.Texto;

        public string ValorFormatado => RelatorioViewModel.Formatar(Valor, Formato);
    }

    public class RelatorioViewModel
    {
        private const string Vazio = "—";

        public string Tipo { get; set; } = TipoRelatorio.Movimentacoes;

        public DateTime DataInicial { get; set; }

        public DateTime DataFinal { get; set; }

        public string Empresa { get; set; } = string.Empty;

        public int ProdutoId { get; set; }

        public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Produtos { get; set; }
            = Enumerable.Empty<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

        public string? FiltroDescricao { get; set; }

        public IReadOnlyList<RelatorioColunaViewModel> Colunas { get; set; }
            = Array.Empty<RelatorioColunaViewModel>();

        public IReadOnlyList<object?[]> Linhas { get; set; }
            = Array.Empty<object?[]>();

        public IReadOnlyList<RelatorioTotalViewModel> Totais { get; set; }
            = Array.Empty<RelatorioTotalViewModel>();

        public string? Erro { get; set; }

        public bool PeriodoValido => Erro is null;

        public string Titulo => Tipo switch
        {
            TipoRelatorio.Vendas => "Relatório de Vendas",
            TipoRelatorio.Despesas => "Relatório de Despesas",
            _ => "Relatório de Movimentações"
        };

        public string Periodo => $"{DataInicial:dd/MM/yyyy} a {DataFinal:dd/MM/yyyy}";

        public bool PermiteFiltroProduto => Tipo == TipoRelatorio.Movimentacoes;

        public bool TemLinhas => Linhas.Count > 0;

        public string NomeArquivo =>
            $"relatorio-{Tipo}-{DataInicial:yyyyMMdd}-a-{DataFinal:yyyyMMdd}.xlsx";

        public string MensagemVazia => Tipo switch
        {
            TipoRelatorio.Vendas => "Nenhuma venda registrada no período selecionado.",
            TipoRelatorio.Despesas => "Nenhuma despesa registrada no período selecionado.",
            _ when ProdutoId > 0 => "Nenhuma movimentação registrada para o produto e período selecionados.",
            _ => "Nenhuma movimentação registrada no período selecionado."
        };

        public string MensagemTabelaVazia => PeriodoValido
            ? MensagemVazia
            : "Corrija o período informado para exibir os dados.";

        public static string Formatar(object? valor, FormatoColuna formato) => valor switch
        {
            null => Vazio,
            decimal numero when formato == FormatoColuna.Moeda => numero.ToString("C"),
            int inteiro when formato == FormatoColuna.Inteiro => inteiro.ToString("N0"),
            DateTime data when formato == FormatoColuna.Data => data.ToString("dd/MM/yyyy"),
            DateTime data when formato == FormatoColuna.DataHora => data.ToString("dd/MM/yyyy HH:mm"),
            _ => valor.ToString() ?? Vazio
        };
    }
}
