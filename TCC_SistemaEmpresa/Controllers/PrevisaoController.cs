using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;
using TCC_SistemaEmpresa.Services;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE")]
    public class PrevisaoController :ControllerValidacao
    {
        private readonly ILogger<PrevisaoController> _logger;
        private readonly PrevisaoService _previsao;

        private const int MesesHistoricoPadrao = 24;
        private const int MesesPrevisaoPadrao = 6;
        private const int MesesPrevisaoMaximo = 24;
        private const string SerieFaturamento = "faturamento";
        private const string SerieDespesas = "despesas";

        public PrevisaoController(
            AppDbContext context,
            PrevisaoService previsao,
            ILogger<PrevisaoController> logger)
            : base(context)
        {
            _previsao = previsao;
            _logger = logger;
        }

        protected override string EntidadeLog => nameof(Venda);

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? dataInicial,
            DateTime? dataFinal,
            int? mesesPrevisao)
        {
            var empresaId = EmpresaIdAtual();
            var fim = dataFinal?.Date ?? DateTime.Today;
            var inicio = dataInicial?.Date
                ?? new DateTime(fim.Year, fim.Month, 1).AddMonths(-(MesesHistoricoPadrao - 1));

            if (inicio > fim)
                (inicio, fim) = (fim, inicio);

            var horizonte = Math.Clamp(mesesPrevisao ?? MesesPrevisaoPadrao, 1, MesesPrevisaoMaximo);
            var fimExclusivo = fim.AddDays(1); //necessario por causa de venda ser datetime

            var faturamentoPorMes = await _context.Vendas
                .AsNoTracking()
                .Where(v => v.EmpresaId == empresaId
                && v.SituacaoVenda == SituacaoVenda.Concluida
                && v.DataVenda >= inicio
                && v.DataVenda < fimExclusivo)
                .GroupBy(v => new { v.DataVenda.Year, v.DataVenda.Month })
                .Select(grupo => new
                {
                    grupo.Key.Year,
                    grupo.Key.Month,
                    Total = grupo.Sum(v => v.ValorFinal)
                })
                .ToListAsync();

            var despesasPorMes = await _context.Despesas
                .AsNoTracking()
                .Where(d => d.EmpresaId == empresaId
                && d.DataDespesa >= inicio
                && d.DataDespesa <= fim)
                .GroupBy(d => new { d.DataDespesa.Year, d.DataDespesa.Month })
                .Select(grupo => new
                {
                    grupo.Key.Year,
                    grupo.Key.Month,
                    Total = grupo.Sum(d => d.Valor)
                })
                .ToListAsync();

            var faturamentoHistorico = MontarSerieMensal(inicio, fim,
                faturamentoPorMes.ToDictionary(f => (f.Year, f.Month), f => f.Total));

            var despesasHistorico = MontarSerieMensal(inicio, fim,
                despesasPorMes.ToDictionary(d => (d.Year, d.Month), d => d.Total));

            var model = new PrevisaoViewModel //se o python n funcionar, ainda vai mostrar o historico e n uma pag em branco
            {
                PeriodoInicial = inicio,
                PeriodoFinal = fim,
                MesesPrevisao = horizonte,
                FaturamentoHistorico = faturamentoHistorico,
                DespesasHistorico = despesasHistorico
            };

            if (faturamentoHistorico.Count >= 3)
            {
                var requisicao = new RequisicaoPrevisaoDto
                {
                    MesesPrevisao = horizonte,
                    Series = new List<SerieHistoricaDto>
                    {
                        new()
                        {
                            Nome = SerieFaturamento,
                            Pontos = faturamentoHistorico.Select(ConverterParaDto).ToList()
                        },
                        new()
                        {
                            Nome = SerieDespesas,
                            Pontos = despesasHistorico.Select(ConverterParaDto).ToList()
                        }
                    }
                };
                var resposta = await _previsao.PreverAsync(requisicao, HttpContext.RequestAborted);

                if (resposta is null)
                {
                    model.ServicoIndisponivel = true;

                    _logger.LogWarning("Previsão indisponivel para a empresa {EmpresaId} serviço analitico nao respondeu",
                        empresaId);
                }
                else
                {
                    model.FaturamentoPrevisto = ConverterParaViewModel(resposta.Series.FirstOrDefault(
                        s => string.Equals(s.Nome, SerieFaturamento, StringComparison.OrdinalIgnoreCase)));

                    model.DespesasPrevistas = ConverterParaViewModel(resposta.Series.FirstOrDefault(
                        s => string.Equals(s.Nome, SerieDespesas, StringComparison.OrdinalIgnoreCase)));

                    _logger.LogInformation(
                        "Previsão gerada para a empresa {EmpresaId}: {Meses} mêses a partir de {Historico} meses de historico",
                        empresaId, horizonte, faturamentoHistorico.Count);
                }
            }
            return View(model);
        }

        private static PontoSerieDto ConverterParaDto(PontoMensalViewModel ponto) => new()
        {
            Ano = ponto.Ano,
            Mes = ponto.Mes,
            Valor = ponto.Total
        };

        private static SerieProjetadaViewModel? ConverterParaViewModel(SeriePrevistaDto? dto)
        {
            if (dto is null)
                return null;

            return new SerieProjetadaViewModel
            {
                Suficiente = dto.Suficiente,
                CoeficienteAngular = dto.CoeficienteAngular,
                R2 = dto.R2,
                Pontos = dto.Pontos.Select(p => new PontoMensalViewModel
                {
                    Ano = p.Ano,
                    Mes = p.Mes,
                    Total = p.Valor
                }).ToList()
            };
        }

        private static List<PontoMensalViewModel> MontarSerieMensal(
            DateTime inicio,
            DateTime fim,
            IReadOnlyDictionary<(int Ano, int Mes), decimal> totais)
        {
            var serie = new List<PontoMensalViewModel>();
            var cursor = new DateTime(inicio.Year, inicio.Month, 1);
            var ultimo = new DateTime(fim.Year, fim.Month, 1);

            while (cursor <= ultimo)
            {
                var chave = (cursor.Year, cursor.Month);
                var total = totais.TryGetValue(chave, out var valor) ? valor : 0m;

                serie.Add(new PontoMensalViewModel
                {
                    Ano = cursor.Year,
                    Mes = cursor.Month,
                    Total = total
                });

                cursor = cursor.AddMonths(1);
            }
            return serie;
        }
    }
}
