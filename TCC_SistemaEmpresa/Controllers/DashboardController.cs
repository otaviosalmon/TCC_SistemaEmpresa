using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE")]
    public class DashboardController : ControllerValidacao
    {
        private readonly ILogger<DashboardController> _logger;
        private const int MesesPadrao = 12;

        private const int TopProdutos = 5;

        public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
            : base(context)
        {
            _logger = logger;
        }

        protected override string EntidadeLog => nameof(Venda);

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? dataInicial, DateTime? dataFinal)
        {
            var empresaId = EmpresaIdAtual();

            var fim = dataFinal?.Date ?? DateTime.Today;
            var inicio = dataInicial?.Date ?? new DateTime(fim.Year, fim.Month, 1).AddMonths(-(MesesPadrao - 1));

            if (inicio > fim)
                (inicio, fim) = (fim, inicio);

            var fimExclusivo = fim.AddDays(1);

            var vendasConcluidas = _context.Vendas
                .AsNoTracking()
                .Where(v => v.EmpresaId == empresaId && v.SituacaoVenda == SituacaoVenda.Concluida && v.DataVenda >= inicio && v.DataVenda < fimExclusivo);

            var receitaBruta = await vendasConcluidas
                .SumAsync(v => (decimal?)v.ValorFinal) ?? 0m;
            var quantidadeVendas = await vendasConcluidas.CountAsync();

            var custoProdutos = await _context.ItensVenda //é necessario o join com tb_venda para filtrar por empresa
                .AsNoTracking()
                .Where(i => i.Venda.EmpresaId == empresaId && i.Venda.SituacaoVenda == SituacaoVenda.Concluida && i.Venda.DataVenda >= inicio && i.Venda.DataVenda < fimExclusivo)
                .SumAsync(i => (decimal?)(i.PrecoCusto ?? i.Produto.PrecoCusto * i.Quantidade)) ?? 0m;

            var totalDespesas = await _context.Despesas
                .AsNoTracking()
                .Where(d => d.EmpresaId == empresaId && d.DataDespesa >= inicio && d.DataDespesa <= fim)
                .SumAsync(d => (decimal?)d.Valor) ?? 0m;

            var produtosMaisVendidos = await _context.ItensVenda
                .AsNoTracking()
                .Where(i => i.Venda.EmpresaId == empresaId && i.Venda.SituacaoVenda == SituacaoVenda.Concluida && i.Venda.DataVenda >= inicio && i.Venda.DataVenda < fimExclusivo)
                .GroupBy(i => new { i.ProdutoId, i.Produto.Nome })
                .Select(grupo => new ProdutoMaisVendidoViewModel
                {
                    Produto = grupo.Key.Nome,
                    Quantidade = grupo.Sum(i => i.Quantidade),
                    ValorVendido = grupo.Sum(i => i.Subtotal)
                })
                .OrderByDescending(p => p.Quantidade)
                .Take(TopProdutos)
                .ToListAsync();

            var faturamentoBruto = await vendasConcluidas
                .GroupBy(v => new { v.DataVenda.Year, v.DataVenda.Month })
                .Select(grupo => new
                {
                    grupo.Key.Year,
                    grupo.Key.Month,
                    Total = grupo.Sum(v => v.ValorFinal)
                })
                .ToListAsync();

            var evolucao = MontarSerieMensal(inicio, fim,
                faturamentoBruto.ToDictionary(f => (f.Year, f.Month), f => f.Total));

            var model = new DashboardViewModel
            {
                PeriodoInicial = inicio,
                PeriodoFinal = fim,
                ReceitaBruta = receitaBruta,
                CustoProdutosVendidos = custoProdutos,
                TotalDespesas = totalDespesas,
                QuantidadeVendas = quantidadeVendas,
                ProdutosMaisVendidos = produtosMaisVendidos,
                EvolucaoFaturamento = evolucao
            };

            _logger.LogInformation(
                "Dashboard da empresa {EmpresaId} carregado para o periodo {Inicio:d} a {Fim:d}.", empresaId, inicio, fim);

            return View(model);
        }
        private static List<FaturamentoMensalViewModel> MontarSerieMensal(
           DateTime inicio, DateTime fim, IReadOnlyDictionary<(int Ano, int Mes), decimal> totais)
        {
            var serie = new List<FaturamentoMensalViewModel>();

            var cursor = new DateTime(inicio.Year, inicio.Month, 1);
            var ultimo = new DateTime(fim.Year, fim.Month, 1);

            while (cursor <= ultimo)
            {
                var chave = (cursor.Year, cursor.Month);
                var total = totais.TryGetValue(chave, out var valor) ? valor : 0m;

                serie.Add(new FaturamentoMensalViewModel
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
