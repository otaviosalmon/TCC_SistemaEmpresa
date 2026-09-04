using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models.ViewModels;
using TCC_SistemaEmpresa.Security;
using TCC_SistemaEmpresa.Services;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE")]
    public class RelatoriosController : ControllerValidacao
    {
        private readonly ILogger<RelatoriosController> _logger;

        public RelatoriosController(AppDbContext context, ILogger<RelatoriosController> logger)
            : base(context)
        {
            _logger = logger;
        }

        protected override string EntidadeLog => "Relatorio";

        private static readonly RelatorioColunaViewModel[] ColunasMovimentacoes =
        {
            new() { Titulo = "Id", Formato = FormatoColuna.Inteiro },
            new() { Titulo = "Data", Formato = FormatoColuna.DataHora },
            new() { Titulo = "Produto" },
            new() { Titulo = "Tipo" },
            new() { Titulo = "Natureza" },
            new() { Titulo = "Quantidade", Formato = FormatoColuna.Inteiro },
            new() { Titulo = "Qtd Antes", Formato = FormatoColuna.Inteiro },
            new() { Titulo = "Qtd Depois", Formato = FormatoColuna.Inteiro },
            new() { Titulo = "Usuário" },
            new() { Titulo = "Venda", Formato = FormatoColuna.Inteiro },
            new() { Titulo = "Observação" }
        };

        private static readonly RelatorioColunaViewModel[] ColunasVendas =
        {
            new() { Titulo = "Id", Formato = FormatoColuna.Inteiro },
            new() { Titulo = "Data", Formato = FormatoColuna.DataHora },
            new() { Titulo = "Funcionário" },
            new() { Titulo = "Cliente" },
            new() { Titulo = "Forma de Pagamento" },
            new() { Titulo = "Itens", Formato = FormatoColuna.Inteiro },
            new() { Titulo = "Qtd Produtos", Formato = FormatoColuna.Inteiro },
            new() { Titulo = "Valor Total", Formato = FormatoColuna.Moeda },
            new() { Titulo = "Desconto", Formato = FormatoColuna.Moeda },
            new() { Titulo = "Valor Final", Formato = FormatoColuna.Moeda },
            new() { Titulo = "Situação" }
        };

        private static readonly RelatorioColunaViewModel[] ColunasDespesas =
        {
            new() { Titulo = "Id", Formato = FormatoColuna.Inteiro },
            new() { Titulo = "Data", Formato = FormatoColuna.Data },
            new() { Titulo = "Categoria" },
            new() { Titulo = "Descrição" },
            new() { Titulo = "Recorrência" },
            new() { Titulo = "Responsável" },
            new() { Titulo = "Valor", Formato = FormatoColuna.Moeda },
            new() { Titulo = "Observação" }
        };

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tipo, DateTime? dataInicial, DateTime? dataFinal, int produtoId = 0)
        {
            return View(await MontarAsync(tipo, dataInicial, dataFinal, produtoId));
        }

        [HttpGet]
        public async Task<IActionResult> Exportar(
            string? tipo, DateTime? dataInicial, DateTime? dataFinal, int produtoId = 0)
        {
            var relatorio = await MontarAsync(tipo, dataInicial, dataFinal, produtoId);

            if (!relatorio.PeriodoValido)
            {
                TempData["Erro"] = relatorio.Erro;
                return RedirectToAction(nameof(Index), RotaDoRelatorio(relatorio));
            }

            if (!relatorio.TemLinhas)
            {
                TempData["Erro"] = $"{relatorio.MensagemVazia} Não há o que exportar.";
                return RedirectToAction(nameof(Index), RotaDoRelatorio(relatorio));
            }

            var planilha = PlanilhaRelatorio.Gerar(relatorio);

            var filtro = relatorio.FiltroDescricao is null ? string.Empty : $", {relatorio.FiltroDescricao}";

            RegistrarLog("EXPORTACAO", null,
                $"{relatorio.Titulo} exportado em Excel: período {relatorio.Periodo}{filtro}, {relatorio.Linhas.Count} registro(s).");
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Relatório {Tipo} da empresa {EmpresaId} exportado por {Usuario}: período {Inicio:d} a {Fim:d}, {Registros} registro(s).",
                relatorio.Tipo, EmpresaIdAtual(), User.Identity?.Name,
                relatorio.DataInicial, relatorio.DataFinal, relatorio.Linhas.Count);

            return File(planilha, PlanilhaRelatorio.TipoConteudo, relatorio.NomeArquivo);
        }

        private async Task<RelatorioViewModel> MontarAsync(
            string? tipo, DateTime? dataInicial, DateTime? dataFinal, int produtoId)
        {
            tipo = TipoRelatorio.Normalizar(tipo);

            var erro = ValidarPeriodo(dataInicial, dataFinal);

            var fim = dataFinal?.Date ?? DateTime.Today;
            var inicio = dataInicial?.Date ?? new DateTime(fim.Year, fim.Month, 1);

            var produtos = tipo == TipoRelatorio.Movimentacoes
                ? await CarregarProdutosAsync()
                : new List<ProdutoFiltroViewModel>();

            var produtoSelecionado = produtos.FirstOrDefault(p => p.Id == produtoId);
            produtoId = produtoSelecionado?.Id ?? 0;

            var relatorio = erro is null
                ? tipo switch
                {
                    TipoRelatorio.Vendas => await MontarVendasAsync(inicio, fim),
                    TipoRelatorio.Despesas => await MontarDespesasAsync(inicio, fim),
                    _ => await MontarMovimentacoesAsync(inicio, fim, produtoId)
                }
                : new RelatorioViewModel { Colunas = ColunasDe(tipo), Erro = erro };

            relatorio.Tipo = tipo;
            relatorio.DataInicial = inicio;
            relatorio.DataFinal = fim;
            relatorio.Empresa = User.FindFirst(ClaimsEmpresa.EmpresaNome)?.Value ?? string.Empty;
            relatorio.ProdutoId = produtoId;
            relatorio.Produtos = produtos.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Nome,
                Selected = p.Id == produtoId
            });
            relatorio.FiltroDescricao = produtoSelecionado is null
                ? null
                : $"Produto: {produtoSelecionado.Nome}";

            return relatorio;
        }

        private Task<List<ProdutoFiltroViewModel>> CarregarProdutosAsync()
        {
            var empresaId = EmpresaIdAtual();

            return _context.Produtos
                .AsNoTracking()
                .Where(p => p.EmpresaId == empresaId)
                .OrderBy(p => p.Nome)
                .Select(p => new ProdutoFiltroViewModel { Id = p.Id, Nome = p.Nome })
                .ToListAsync();
        }

        private async Task<RelatorioViewModel> MontarMovimentacoesAsync(
            DateTime inicio, DateTime fim, int produtoId)
        {
            var empresaId = EmpresaIdAtual();
            var fimExclusivo = fim.AddDays(1);

            var movimentacoes = await _context.MovimentacoesEstoque
                .AsNoTracking()
                .Where(m => m.EmpresaId == empresaId
                    && m.DataMovimentacao >= inicio
                    && m.DataMovimentacao < fimExclusivo
                    && (produtoId == 0 || m.ProdutoId == produtoId))
                .OrderBy(m => m.DataMovimentacao)
                .ThenBy(m => m.Id)
                .Select(m => new
                {
                    m.Id,
                    m.DataMovimentacao,
                    Produto = m.Produto.Nome,
                    Tipo = m.TipoMovimentacao.Nome,
                    m.TipoMovimentacao.Natureza,
                    m.Quantidade,
                    m.QuantidadeAntes,
                    m.QuantidadeDepois,
                    Usuario = m.Usuario.Username,
                    m.VendaId,
                    m.Observacao
                })
                .ToListAsync();

            var entradas = movimentacoes
                .Where(m => m.Natureza == NaturezaMovimentacao.Entrada)
                .Sum(m => m.Quantidade);

            var saidas = movimentacoes
                .Where(m => m.Natureza == NaturezaMovimentacao.Saida)
                .Sum(m => m.Quantidade);

            return new RelatorioViewModel
            {
                Colunas = ColunasMovimentacoes,
                Linhas = movimentacoes
                    .Select(m => new object?[]
                    {
                        m.Id,
                        m.DataMovimentacao,
                        m.Produto,
                        m.Tipo,
                        NaturezaMovimentacao.Rotulo(m.Natureza),
                        m.Quantidade,
                        m.QuantidadeAntes,
                        m.QuantidadeDepois,
                        m.Usuario,
                        m.VendaId,
                        m.Observacao
                    })
                    .ToList(),
                Totais = new[]
                {
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Movimentações no período",
                        Valor = movimentacoes.Count,
                        Formato = FormatoColuna.Inteiro
                    },
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Total de entradas (un.)",
                        Valor = entradas,
                        Formato = FormatoColuna.Inteiro
                    },
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Total de saídas (un.)",
                        Valor = saidas,
                        Formato = FormatoColuna.Inteiro
                    },
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Saldo do período (un.)",
                        Valor = entradas - saidas,
                        Formato = FormatoColuna.Inteiro
                    }
                }
            };
        }

        private async Task<RelatorioViewModel> MontarVendasAsync(DateTime inicio, DateTime fim)
        {
            var empresaId = EmpresaIdAtual();
            var fimExclusivo = fim.AddDays(1);

            var vendas = await _context.Vendas
                .AsNoTracking()
                .Where(v => v.EmpresaId == empresaId
                    && v.DataVenda >= inicio
                    && v.DataVenda < fimExclusivo)
                .OrderBy(v => v.DataVenda)
                .ThenBy(v => v.Id)
                .Select(v => new
                {
                    v.Id,
                    v.DataVenda,
                    Funcionario = v.Funcionario.Nome,
                    Cliente = v.Cliente != null ? v.Cliente.Nome : null,
                    FormaPagamento = v.FormaPagamento.Nome,
                    Itens = _context.ItensVenda.Count(i => i.VendaId == v.Id),
                    Quantidade = _context.ItensVenda
                        .Where(i => i.VendaId == v.Id)
                        .Sum(i => (int?)i.Quantidade) ?? 0,
                    v.ValorTotal,
                    v.Desconto,
                    v.ValorFinal,
                    v.SituacaoVenda
                })
                .ToListAsync();

            var concluidas = vendas.Where(v => v.SituacaoVenda == SituacaoVenda.Concluida).ToList();

            return new RelatorioViewModel
            {
                Colunas = ColunasVendas,
                Linhas = vendas
                    .Select(v => new object?[]
                    {
                        v.Id,
                        v.DataVenda,
                        v.Funcionario,
                        v.Cliente ?? "Não identificado",
                        v.FormaPagamento,
                        v.Itens,
                        v.Quantidade,
                        v.ValorTotal,
                        v.Desconto,
                        v.ValorFinal,
                        RotuloSituacao(v.SituacaoVenda)
                    })
                    .ToList(),
                Totais = new[]
                {
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Vendas concluídas",
                        Valor = concluidas.Count,
                        Formato = FormatoColuna.Inteiro
                    },
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Vendas canceladas",
                        Valor = vendas.Count - concluidas.Count,
                        Formato = FormatoColuna.Inteiro
                    },
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Descontos concedidos",
                        Valor = concluidas.Sum(v => v.Desconto),
                        Formato = FormatoColuna.Moeda
                    },
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Faturamento (concluídas)",
                        Valor = concluidas.Sum(v => v.ValorFinal),
                        Formato = FormatoColuna.Moeda
                    }
                }
            };
        }

        private async Task<RelatorioViewModel> MontarDespesasAsync(DateTime inicio, DateTime fim)
        {
            var empresaId = EmpresaIdAtual();

            var despesas = await _context.Despesas
                .AsNoTracking()
                .Where(d => d.EmpresaId == empresaId
                    && d.DataDespesa >= inicio
                    && d.DataDespesa <= fim)
                .OrderBy(d => d.DataDespesa)
                .ThenBy(d => d.Id)
                .Join(_context.Usuario.AsNoTracking(),
                    despesa => despesa.UsuarioId,
                    usuario => usuario.Id,
                    (despesa, usuario) => new
                    {
                        despesa.Id,
                        despesa.DataDespesa,
                        Categoria = despesa.CategoriaDespesa.Nome,
                        despesa.Descricao,
                        despesa.Valor,
                        despesa.Fixa,
                        Responsavel = usuario.Username,
                        despesa.Observacao
                    })
                .ToListAsync();

            var fixas = despesas.Where(d => d.Fixa).Sum(d => d.Valor);
            var eventuais = despesas.Where(d => !d.Fixa).Sum(d => d.Valor);

            return new RelatorioViewModel
            {
                Colunas = ColunasDespesas,
                Linhas = despesas
                    .Select(d => new object?[]
                    {
                        d.Id,
                        d.DataDespesa,
                        d.Categoria,
                        d.Descricao,
                        d.Fixa ? "Fixa" : "Eventual",
                        d.Responsavel,
                        d.Valor,
                        d.Observacao
                    })
                    .ToList(),
                Totais = new[]
                {
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Despesas no período",
                        Valor = despesas.Count,
                        Formato = FormatoColuna.Inteiro
                    },
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Total de despesas fixas",
                        Valor = fixas,
                        Formato = FormatoColuna.Moeda
                    },
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Total de despesas eventuais",
                        Valor = eventuais,
                        Formato = FormatoColuna.Moeda
                    },
                    new RelatorioTotalViewModel
                    {
                        Rotulo = "Total geral",
                        Valor = fixas + eventuais,
                        Formato = FormatoColuna.Moeda
                    }
                }
            };
        }

        private string? ValidarPeriodo(DateTime? dataInicial, DateTime? dataFinal)
        {
            var inicialIlegivel = DataIlegivel(nameof(dataInicial));
            var finalIlegivel = DataIlegivel(nameof(dataFinal));

            if (inicialIlegivel && finalIlegivel)
                return "As datas inicial e final são inválidas. Informe datas reais no formato dd/mm/aaaa.";

            if (inicialIlegivel)
                return "A data inicial é inválida. Informe uma data real no formato dd/mm/aaaa.";

            if (finalIlegivel)
                return "A data final é inválida. Informe uma data real no formato dd/mm/aaaa.";

            if (dataInicial?.Date > dataFinal?.Date)
                return "Período inválido: a data inicial não pode ser maior que a data final.";

            return null;
        }

        private bool DataIlegivel(string campo) =>
            ModelState.GetFieldValidationState(campo) == ModelValidationState.Invalid;

        private static object RotaDoRelatorio(RelatorioViewModel relatorio) => new
        {
            tipo = relatorio.Tipo,
            dataInicial = relatorio.DataInicial.ToString("yyyy-MM-dd"),
            dataFinal = relatorio.DataFinal.ToString("yyyy-MM-dd"),
            produtoId = relatorio.ProdutoId
        };

        private static IReadOnlyList<RelatorioColunaViewModel> ColunasDe(string tipo) => tipo switch
        {
            TipoRelatorio.Vendas => ColunasVendas,
            TipoRelatorio.Despesas => ColunasDespesas,
            _ => ColunasMovimentacoes
        };

        private static string RotuloSituacao(string? situacao) => situacao switch
        {
            SituacaoVenda.Cancelada => "Cancelada",
            _ => "Concluída"
        };
    }
}
