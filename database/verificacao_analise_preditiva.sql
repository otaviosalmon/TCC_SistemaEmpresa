
DECLARE @inicio       DATE = '2025-10-01';

DECLARE @fimExclusivo DATE = '2026-09-11';
DECLARE @emp INT = (SELECT id FROM Tb_Empresa WHERE cnpj = '11222333000181');

SELECT

    Vendas = (SELECT COUNT(*) FROM Tb_Venda v
               WHERE v.empresa_id = @emp                      
                 AND v.situacao_venda = 'CONCLUIDA'           
                 AND v.data_venda >= @inicio AND v.data_venda < @fimExclusivo),

    -- Receita Bruta: soma de valor_final (já descontado), igual a SumAsync(v => v.ValorFinal)
    ReceitaBruta = (SELECT ISNULL(SUM(v.valor_final), 0) FROM Tb_Venda v
                     WHERE v.empresa_id = @emp
                       AND v.situacao_venda = 'CONCLUIDA'
                       AND v.data_venda >= @inicio AND v.data_venda < @fimExclusivo),

    -- CMV: (snapshot ?? custo atual) * quantidade, a fórmula já CORRIGIDA da linha 50
    CMV = (SELECT ISNULL(SUM(ISNULL(iv.preco_custo, p.preco_custo) * iv.quantidade), 0)
             FROM Tb_Item_Venda iv
             JOIN Tb_Venda   v ON v.id = iv.venda_id
             -- Fallback do custo para itens antigos, anteriores ao ALTER TABLE do preco_custo
             JOIN Tb_Produto p ON p.id = iv.produto_id
            WHERE v.empresa_id = @emp
              AND v.situacao_venda = 'CONCLUIDA'
              AND v.data_venda >= @inicio AND v.data_venda < @fimExclusivo),

    Despesas = (SELECT ISNULL(SUM(d.valor), 0) FROM Tb_Despesa d
                 WHERE d.empresa_id = @emp
                   AND d.data_despesa >= @inicio AND d.data_despesa < @fimExclusivo);