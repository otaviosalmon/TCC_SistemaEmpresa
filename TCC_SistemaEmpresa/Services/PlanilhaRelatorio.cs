using ClosedXML.Excel;
using TCC_SistemaEmpresa.Models.ViewModels;

namespace TCC_SistemaEmpresa.Services
{
    public static class PlanilhaRelatorio
    {
        public const string TipoConteudo =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private const int LinhaTitulo = 1;
        private const int PrimeiraLinhaInformacao = 2;

        private static readonly XLColor FundoCabecalho = XLColor.FromHtml("#1E1E1E");
        private static readonly XLColor FundoTotais = XLColor.FromHtml("#EDEDED");

        public static byte[] Gerar(RelatorioViewModel relatorio)
        {
            using var pasta = new XLWorkbook();
            var planilha = pasta.Worksheets.Add(NomeDaAba(relatorio.Tipo));
            var ultimaColuna = Math.Max(relatorio.Colunas.Count, 1);

            var linhaCabecalho = EscreverCabecalhoDoDocumento(planilha, relatorio, ultimaColuna);
            EscreverColunas(planilha, relatorio, linhaCabecalho);

            var proximaLinha = EscreverLinhas(planilha, relatorio, linhaCabecalho);
            EscreverTotais(planilha, relatorio, proximaLinha, ultimaColuna);

            planilha.SheetView.FreezeRows(linhaCabecalho);
            planilha.Columns().AdjustToContents(linhaCabecalho, linhaCabecalho + relatorio.Linhas.Count);

            using var memoria = new MemoryStream();
            pasta.SaveAs(memoria);
            return memoria.ToArray();
        }

        private static int EscreverCabecalhoDoDocumento(
            IXLWorksheet planilha, RelatorioViewModel relatorio, int ultimaColuna)
        {
            planilha.Cell(LinhaTitulo, 1).Value = relatorio.Titulo;
            planilha.Range(LinhaTitulo, 1, LinhaTitulo, ultimaColuna).Merge();
            planilha.Cell(LinhaTitulo, 1).Style.Font.SetBold().Font.SetFontSize(14);

            var informacoes = new List<string>
            {
                $"Empresa: {relatorio.Empresa}",
                $"Período: {relatorio.Periodo}"
            };

            if (!string.IsNullOrWhiteSpace(relatorio.FiltroDescricao))
                informacoes.Add(relatorio.FiltroDescricao);

            informacoes.Add($"Emitido em {DateTime.Now:dd/MM/yyyy HH:mm}");

            var linha = PrimeiraLinhaInformacao;

            foreach (var informacao in informacoes)
            {
                planilha.Cell(linha, 1).Value = informacao;
                planilha.Range(linha, 1, linha, ultimaColuna).Merge();
                linha++;
            }

            planilha.Range(PrimeiraLinhaInformacao, 1, linha - 1, ultimaColuna)
                .Style.Font.SetFontSize(10).Font.SetFontColor(XLColor.DimGray);

            return linha + 1;
        }

        private static void EscreverColunas(
            IXLWorksheet planilha, RelatorioViewModel relatorio, int linhaCabecalho)
        {
            for (var indice = 0; indice < relatorio.Colunas.Count; indice++)
            {
                var celula = planilha.Cell(linhaCabecalho, indice + 1);
                celula.Value = relatorio.Colunas[indice].Titulo;
                celula.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
                celula.Style.Fill.SetBackgroundColor(FundoCabecalho);
                celula.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
        }

        private static int EscreverLinhas(
            IXLWorksheet planilha, RelatorioViewModel relatorio, int linhaCabecalho)
        {
            var linha = linhaCabecalho + 1;

            foreach (var valores in relatorio.Linhas)
            {
                for (var indice = 0; indice < relatorio.Colunas.Count; indice++)
                {
                    var celula = planilha.Cell(linha, indice + 1);
                    celula.Value = ParaCelula(valores.ElementAtOrDefault(indice));
                    AplicarFormato(celula, relatorio.Colunas[indice].Formato);
                }

                linha++;
            }

            if (relatorio.Linhas.Count > 0)
            {
                planilha.Range(linhaCabecalho, 1, linha - 1, relatorio.Colunas.Count)
                    .Style.Border.SetBottomBorder(XLBorderStyleValues.Hair)
                    .Border.SetBottomBorderColor(XLColor.LightGray);
            }
            else
            {
                planilha.Cell(linha, 1).Value = relatorio.MensagemVazia;
                planilha.Cell(linha, 1).Style.Font.SetItalic().Font.SetFontColor(XLColor.DimGray);
                linha++;
            }

            return linha;
        }

        private static void EscreverTotais(
            IXLWorksheet planilha, RelatorioViewModel relatorio, int primeiraLinha, int ultimaColuna)
        {
            if (relatorio.Totais.Count == 0)
                return;

            var linha = primeiraLinha + 1;
            var colunaValor = Math.Max(2, Math.Min(4, ultimaColuna));
            var ultimaColunaRotulo = colunaValor - 1;

            foreach (var total in relatorio.Totais)
            {
                var rotulo = planilha.Cell(linha, 1);
                rotulo.Value = total.Rotulo;
                rotulo.Style.Font.SetBold();

                if (ultimaColunaRotulo > 1)
                    planilha.Range(linha, 1, linha, ultimaColunaRotulo).Merge();

                var celula = planilha.Cell(linha, colunaValor);
                celula.Value = ParaCelula(total.Valor);
                AplicarFormato(celula, total.Formato);
                celula.Style.Font.SetBold();

                linha++;
            }

            planilha.Range(primeiraLinha + 1, 1, linha - 1, colunaValor)
                .Style.Fill.SetBackgroundColor(FundoTotais);
        }

        private static void AplicarFormato(IXLCell celula, FormatoColuna formato)
        {
            switch (formato)
            {
                case FormatoColuna.Inteiro:
                    celula.Style.NumberFormat.SetFormat("#,##0");
                    celula.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                    break;
                case FormatoColuna.Moeda:
                    celula.Style.NumberFormat.SetFormat("R$ #,##0.00");
                    celula.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                    break;
                case FormatoColuna.Data:
                    celula.Style.DateFormat.SetFormat("dd/mm/yyyy");
                    celula.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    break;
                case FormatoColuna.DataHora:
                    celula.Style.DateFormat.SetFormat("dd/mm/yyyy hh:mm");
                    celula.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    break;
            }
        }

        private static XLCellValue ParaCelula(object? valor) => valor switch
        {
            null => Blank.Value,
            string texto => texto,
            int inteiro => inteiro,
            long longo => longo,
            decimal numero => numero,
            double duplo => duplo,
            bool logico => logico,
            DateTime data => data,
            _ => valor.ToString() ?? string.Empty
        };

        private static string NomeDaAba(string tipo) => tipo switch
        {
            TipoRelatorio.Vendas => "Vendas",
            TipoRelatorio.Despesas => "Despesas",
            _ => "Movimentações"
        };
    }
}
