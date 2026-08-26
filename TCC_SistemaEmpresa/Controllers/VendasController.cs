using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using TCC_SistemaEmpresa.Data;
using TCC_SistemaEmpresa.Models;
using TCC_SistemaEmpresa.Models.ViewModels;
using static TCC_SistemaEmpresa.Models.ViewModels.VendaFormViewModel;

namespace TCC_SistemaEmpresa.Controllers
{
    [Authorize(Roles = "ADMIN,GERENTE,VENDEDOR,CAIXA")]
    public class VendasController : ControllerValidacao
    {
        private const string NaturezaSaida = "SAIDA";
        private const string NaturezaEntrada = "ENTRADA";

        private readonly AppDbContext _context;
        private readonly ILogger<VendasController> _logger;

        public VendasController(AppDbContext context, ILogger<VendasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? busca, DateTime? dataInicial, DateTime? dataFinal, string? filtro)
        {
            var empresaId = EmpresaIdAtual();
            filtro = NormalizarFiltro(filtro);

            var consulta = _context.Vendas
                .AsNoTracking()
                .Where(v => v.EmpresaId == empresaId);

            consulta = filtro switch
            {
                FiltroVenda.Concluidas => consulta.Where(v => v.SituacaoVenda == SituacaoVenda.Concluida),
                FiltroVenda.Canceladas => consulta.Where(v => v.SituacaoVenda == SituacaoVenda.Cancelada),
                _ => consulta
            };

            if (dataInicial.HasValue)
            {
                consulta = consulta.Where(v => v.DataVenda.Date >= dataInicial.Value.Date);
            }

            if (dataFinal.HasValue)
            {
                consulta = consulta.Where(v => v.DataVenda.Date <= dataFinal.Value.Date);
            }

            if (!string.IsNullOrEmpty(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(v =>
                    v.Funcionario.Nome.Contains(termo) ||
                    (v.Cliente != null && v.Cliente.Nome.Contains(termo)));
            }

            var vendas = await consulta
                .OrderByDescending(v => v.DataVenda)
                .ThenByDescending(v => v.Id)
                .Select(v => new VendaLinhaViewModel
                {
                    Id = v.Id,
                    DataVenda = v.DataVenda,
                    Funcionario = v.Funcionario.Nome,
                    Cliente = v.Cliente != null ? v.Cliente.Nome : "Não Identificado",
                    FormaPagamento = v.FormaPagamento.Nome,
                    ValorFinal = v.ValorFinal,
                    SituacaoVenda = v.SituacaoVenda
                })
                .ToListAsync();
            var ids = vendas.Select(v => v.Id).ToList();

            var itensPorVenda = await _context.ItensVenda
                .AsNoTracking()
                .Where(i => ids.Contains(i.VendaId) && i.Venda.EmpresaId == empresaId)
                //tecnicamente esse join e algo redundante, cada venda tem id/identidade unica na tabela vendas,
                // entao nenhuma venda tem o mesmo id, tambem tem uma consulta acima filtrada por empresaId,
                // esse join serve pra proteger caso esse bloco seja copiado para outro lugar
                // ou caso a consulta acima de vendas seja alterada
                .GroupBy(i => i.VendaId)
                .Select(grupo => new { VendaId = grupo.Key, Total = grupo.Count() })
                .ToDictionaryAsync(x => x.VendaId, x => x.Total);

            foreach (var venda in vendas)
            {
                venda.QuantidadeItens = itensPorVenda.TryGetValue(venda.Id, out var total) ? total : 0;
            }

            return View(new VendaListaViewModel
            {
                Busca = busca,
                DataInicial = dataInicial,
                DataFinal = dataFinal,
                Filtro = filtro,
                Vendas = vendas
            });
        }
        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var empresaId = EmpresaIdAtual();
            var venda = await _context.Vendas
                .AsNoTracking()
                .Where(v => v.Id == id && v.EmpresaId == empresaId)
                .Select(v => new VendaFormViewModel
                {
                    Id = v.Id,
                    FuncionarioId = v.FuncionarioId,
                    ClienteId = v.ClienteId,
                    FormaPagamentoId = v.FormaPagamentoId,
                    DataVenda = v.DataVenda,
                    Desconto = v.Desconto,
                    Observacao = v.Observacao,
                    ValorTotal = v.ValorTotal,
                    ValorFinal = v.ValorFinal,
                    SituacaoVenda = v.SituacaoVenda,
                    FuncionarioNome = v.Funcionario.Nome,
                    ClienteNome = v.Cliente != null ? v.Cliente.Nome : "Não identificado",
                    FormaPagamentoNome = v.FormaPagamento.Nome,
                    SomenteLeitura = true

                })
                .FirstOrDefaultAsync();

            if (venda is null)
                return NotFound();

            venda.ItensDetalhe = await _context.ItensVenda
                .AsNoTracking()
                .Where(i => i.VendaId == id && i.Venda.EmpresaId == empresaId)
                .OrderBy(i => i.Id)
                .Select(i => new ItemVendaLinhaViewModel
                {
                    Produto = i.Produto.Nome,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario,
                    Subtotal = i.Subtotal
                })
                .ToListAsync();

            return View(venda);

        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new VendaFormViewModel
            {
                ProximoId = await ProximoIdAsync(),
                Funcionarios = await CarregarFuncionariosAsync(),
                Clientes = await CarregarClientesAsync(),
                FormaPagamento = await CarregarFormasPagamentoAsync(),
                ProdutosDisponiveis = await CarregarProdutosDisponiveisAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendaFormViewModel model)
        {
            var empresaId = EmpresaIdAtual();
            var usuarioId = UsuarioIdAtual();
            var itensPreenchidos = (model.Itens ?? new List<ItemVendaFormViewModel>())
                .Where(i => i.ProdutoId > 0)
                .ToList();

            if (itensPreenchidos.Count == 0)
            {
                ModelState.AddModelError(string.Empty,
                    "A venda precisa de no mínimo um produto.");
            }
            var duplicados = itensPreenchidos
                .GroupBy(i => i.ProdutoId)
                .Where(grupo => grupo.Count() > 1)
                .Select(grupo => grupo.Key)
                .ToList();
            if (duplicados.Count > 0)
            {
                ModelState.AddModelError(string.Empty,
                    "Cada um dos produtos pode aparecer apenas em uma linha." +
                    "Caso precise de aumentar ajuste a quantidade na linha existente.");
            }

            if (usuarioId is null)
            {
                ModelState.AddModelError(string.Empty,
                    "Não foi possivel identificar o usuario da sessão para o registro." +
                    "Refaça o login e tente novamente.");
            }

            await ValidarCabecalhoAsync(model, empresaId);

            var produtoIds = itensPreenchidos.Select(i => i.ProdutoId).Distinct().ToList();
            var produtos = await _context.Produtos
                .AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && produtoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            decimal valorTotal = 0m;

            foreach (var item in itensPreenchidos)
            {
                if (!produtos.TryGetValue(item.ProdutoId, out var produto))
                {
                    ModelState.AddModelError(string.Empty,
                        $"Produto informado (id {item.ProdutoId}) não existe nessa empresa.");
                    continue;
                }
                if (!produto.Ativo)
                {
                    ModelState.AddModelError(string.Empty,
                        $"O produto '{produto.Nome} está inativo e não pode ser vendido ");
                    continue;
                }
                if (item.Quantidade > produto.QuantidadeAtual)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Estoque insuficiente para {produto.Nome}: Disponivel {produto.QuantidadeAtual}.");
                    continue;
                }

                valorTotal += produto.PrecoVenda * item.Quantidade;
            }

            if (model.Desconto > valorTotal)
            {
                ModelState.AddModelError(string.Empty,
                    $"O desconto não pode ser maior que o valor da venda.");
            }

            TipoMovimentacao? tipoSaida = null;
            if (itensPreenchidos.Count() > 0)
            {
                tipoSaida = await BuscarTipoPorNaturezaAsync(empresaId, NaturezaSaida, "venda");
                if (tipoSaida == null)
                {
                    ModelState.AddModelError(string.Empty,
                        "Nenhuma movimentação de saída esta cadastrada." +
                        "Cadastre um (ex.: \"Baixa por venda\" antesd e resgistrar.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.ProximoId = await ProximoIdAsync();
                model.Funcionarios = await CarregarFuncionariosAsync();
                model.Clientes = await CarregarClientesAsync();
                model.FormaPagamento = await CarregarFormasPagamentoAsync();
                model.ProdutosDisponiveis = await CarregarProdutosDisponiveisAsync();

                if (model.Itens is null || model.Itens.Count == 0)
                    model.Itens = new() { new ItemVendaFormViewModel() };

                return View(model);
            }

            var valorFinal = valorTotal - model.Desconto;

            var venda = new Venda
            {
                EmpresaId = empresaId,
                FuncionarioId = model.FuncionarioId,
                ClienteId = model.ClienteId,
                FormaPagamentoId = model.FormaPagamentoId,
                DataVenda = DateTime.Now,
                ValorTotal = valorTotal,
                Desconto = model.Desconto,
                ValorFinal = valorFinal,
                Observacao = string.IsNullOrWhiteSpace(model.Observacao) ? null : model.Observacao,
                SituacaoVenda = SituacaoVenda.Concluida
            };

            await using var transacao = await _context.Database.BeginTransactionAsync();

            _context.Vendas.Add(venda);
            await _context.SaveChangesAsync();

            foreach (var item in itensPreenchidos)
            {
                var produto = produtos[item.ProdutoId];
                var quantidadeAntes = produto.QuantidadeAtual;
                var quantidadeDepois = quantidadeAntes - item.Quantidade;

                _context.ItensVenda.Add(new ItemVenda
                {
                    VendaId = venda.Id,
                    ProdutoId = produto.Id,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = produto.PrecoVenda
                });

                _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    EmpresaId = empresaId,
                    ProdutoId = produto.Id,
                    UsuarioId = usuarioId!.Value,
                    VendaId = venda.Id,
                    TipoMovimentacaoEstoqueId = tipoSaida!.Id,
                    Quantidade = item.Quantidade,
                    QuantidadeAntes = quantidadeAntes,
                    QuantidadeDepois = quantidadeDepois,
                    DataMovimentacao = DateTime.Now,
                    Observacao = $"Baixa automática pela venda."
                });

                produto.QuantidadeAtual = quantidadeDepois;
            }

            RegistrarLog("CRIACAO", venda.Id,
                $"Venda Registrada: {itensPreenchidos.Count} iten(s), valor final: {valorFinal:C}.");

            await _context.SaveChangesAsync();

            await transacao.CommitAsync();

            _logger.LogInformation(
                "Venda {VendaId} criada na empresa {EmpresaId} com {QtdItens} iten(s).", venda.Id, empresaId, itensPreenchidos.Count);

            TempData["Sucesso"] = "Venda #{venda.Id} registrada com sucesso.";

            return RedirectToAction(nameof(Index));


        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar([FromRoute] int id)
        {
            var empresaId = EmpresaIdAtual();
            var usuarioId = UsuarioIdAtual();

            var venda = await _context.Vendas
                .AsTracking()
                .FirstOrDefaultAsync(v => v.Id == id && v.EmpresaId == empresaId);

            if (venda is null)
                return NotFound();

            if (venda.SituacaoVenda == SituacaoVenda.Cancelada)
            {
                TempData["Erro"] = $"A venda #{venda.Id} ja foi cancelada.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (usuarioId is null)
            {
                TempData["Erro"] =
                    "Não foi possivel identificar o usuario logado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var movimentacoesOriginais = await _context.MovimentacoesEstoque
                .AsNoTracking()
                .Where(m => m.VendaId == id && m.EmpresaId == empresaId)
                .ToListAsync();

            var tipoEntrada = await BuscarTipoPorNaturezaAsync(empresaId, NaturezaEntrada, "devol");

            if (tipoEntrada is null)
            {
                TempData["Erro"] =
                    "Nenhum tipo de movimentacao de ENTRADA esta cadastrado" +
                    "(ex.: \"Devolução\"). Cadastre um antes.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var produtoIds =  movimentacoesOriginais.Select(m => m.ProdutoId).Distinct().ToList();
            var produtos = await _context.Produtos
                .AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && produtoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            await using var transacao = await _context.Database.BeginTransactionAsync();

            foreach (var movimentacaoOriginal in movimentacoesOriginais)
            {
                if (!produtos.TryGetValue(movimentacaoOriginal.ProdutoId, out var produto))
                {
                    continue; //não é pra acontecer mas se acontecer....
                }
                var quantidadeAntes = produto.QuantidadeAtual;
                var quantidadeDepois = quantidadeAntes + movimentacaoOriginal.Quantidade;

                _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    EmpresaId = empresaId,
                    ProdutoId = produto.Id,
                    UsuarioId = usuarioId!.Value,
                    VendaId = venda.Id,
                    TipoMovimentacaoEstoqueId = tipoEntrada.Id,
                    Quantidade = movimentacaoOriginal.Quantidade,
                    QuantidadeAntes = quantidadeAntes,
                    QuantidadeDepois = quantidadeDepois,
                    DataMovimentacao = DateTime.Now,
                    Observacao = $"Estorno de estoque pelo cancelamento da venda: {venda.Id}"
                });
                produto.QuantidadeAtual = quantidadeDepois;

            }

            venda.SituacaoVenda = SituacaoVenda.Cancelada;

            RegistrarLog("CANCELAMENTO", venda.Id,
                $"Venda {venda.Id} cancelada; estoque de {movimentacoesOriginais.Count} iten(s) estornados.");

            await _context.SaveChangesAsync();

            await transacao.CommitAsync();

            _logger.LogWarning(
                "Venda {VendaId} cancelada por {Usuario}; estoque de {QtdItens} iten(s) estornado.",
                id, User.Identity?.Name, movimentacoesOriginais.Count);

            TempData["Sucesso"] = $"Venda #{venda.Id} cancelada e estoque estornado.";
            return RedirectToAction(nameof(Details), new { id });

        }

        private async Task ValidarCabecalhoAsync(VendaFormViewModel model, int empresaId)
        {
            if (model.FuncionarioId > 0)
            {
                var funcionarioValido = await _context.Funcionarios
                    .AsNoTracking()
                    .AnyAsync(f => f.Id == model.FuncionarioId && f.EmpresaId == empresaId && f.Ativo);

                if (!funcionarioValido)
                    ModelState.AddModelError(nameof(model.FuncionarioId), "Funcionario Inválido.");
            }

            if (model.FormaPagamentoId > 0)
            {
                var formaValida = await _context.FormasPagamento
                    .AsNoTracking()
                    .AnyAsync(f => f.Id == model.FormaPagamentoId && f.EmpresaId == empresaId && f.Ativo);
                if (!formaValida)
                    ModelState.AddModelError(nameof(model.FormaPagamentoId), "Forma de pagamento inválida.");
            }

            if (model.ClienteId.HasValue)
            {
                var clienteValido = await _context.Clientes
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == model.ClienteId && c.EmpresaId == empresaId && c.Ativo);
                if (!clienteValido)
                    ModelState.AddModelError(nameof(model.ClienteId), "Cliente inválido");
            }
        }

        private async Task<TipoMovimentacao?> BuscarTipoPorNaturezaAsync(
            int empresaId, string natureza, string preferenciaNoNome)
        {
            var tipos = await _context.TiposMovimentacao
                .AsNoTracking()
                .Where(t => t.EmpresaId == empresaId && t.Ativo && t.Natureza == natureza)
                .OrderBy(t => t.Id)
                .ToListAsync();

            return tipos.FirstOrDefault(
                t => t.Nome.Contains(preferenciaNoNome, StringComparison.OrdinalIgnoreCase))

                ?? tipos.FirstOrDefault();
        }

        private async Task<int?> ProximoIdAsync()
        {
            const string sql = @"
                SELECT CASE
                    WHEN coluna.last_value IS NULL THEN CONVERT (int, coluna.seed_value)
                    ELSE CONVERT(int, coluna.last_value) + CONVERT(int, coluna.increment_value)
                   END AS Value
                FROM sys.identity.columns AS coluna
                WHERE coluna.object_id = OBJECT_ID('Tb_Venda')";

            try
            {
                return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possivel prever o proximo id de Tb_Venda");
                return null;
            }
        }

        private async Task<IEnumerable<SelectListItem>> CarregarFuncionariosAsync(int? selecionado = null)
        {
            var empresaId = EmpresaIdAtual();

            var funcionarios = await _context.Funcionarios
                .AsNoTracking()
                .Where(f => f.EmpresaId == empresaId && f.Ativo)
                .OrderBy(f => f.Nome)
                .Select(f => new { f.Id, f.Nome })
                .ToListAsync();

            return funcionarios.Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = f.Nome,
                Selected = selecionado.HasValue && f.Id == selecionado.Value
            });
        }

        private async Task<IEnumerable<SelectListItem>> CarregarClientesAsync(int? selecionado = null)
        {
            var empresaId = EmpresaIdAtual();

            var clientes = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.EmpresaId == empresaId && c.Ativo)
                .OrderBy(c => c.Nome)
                .Select(c => new { c.Id, c.Nome })
                .ToListAsync();

            return clientes.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Nome,
                Selected = selecionado.HasValue && c.Id == selecionado.Value
            });
        }

        private async Task<IEnumerable<SelectListItem>> CarregarFormasPagamentoAsync(int? selecionado = null)
        {
            var empresaId = EmpresaIdAtual();

            var formasPagamento = await _context.FormasPagamento
                .AsNoTracking()
                .Where(f => f.EmpresaId == empresaId && f.Ativo)
                .OrderBy(f => f.Nome)
                .Select(f => new { f.Id, f.Nome })
                .ToListAsync();

            return formasPagamento.Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = f.Nome,
                Selected = selecionado.HasValue && f.Id == selecionado.Value
            });
        }

        private async Task<IReadOnlyList<ProdutoVendaOpcaoViewModel>> CarregarProdutosDisponiveisAsync()
        {
            var empresaId = EmpresaIdAtual();

            return await _context.Produtos.
                AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && p.Ativo)
                .OrderBy(p => p.Nome)
                .Select(p => new ProdutoVendaOpcaoViewModel
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    PrecoVenda = p.PrecoVenda,
                    QuantidadeAtual = p.QuantidadeAtual,
                })
                .ToListAsync();
        }

        private void RegistrarLog(string acao, int registroId, string detalhes)
        {
            _context.LogsSistema.Add(new LogSistema
            {
                EmpresaId = EmpresaIdAtual(),
                UsuarioId = UsuarioIdAtual(),
                Acao = acao,
                EntidadeAfetada = nameof(Venda),
                RegistroId = registroId,
                DataHora = DateTime.Now,
                Detalhes = detalhes
            });
        }

        private static string NormalizarFiltro(string? filtro) => filtro switch
        {
            FiltroVenda.Concluidas => FiltroVenda.Concluidas,
            FiltroVenda.Canceladas => FiltroVenda.Canceladas,
            _ => FiltroVenda.Todas
        };



    }
}

    


