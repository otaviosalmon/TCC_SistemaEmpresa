-- ================================================================
--  SEED DE TESTE — Categorias de despesa e despesas
--  Empresa 1 / usuário 'Teste'. Pode ser executado mais de uma vez.
--  Para limpar: DELETE FROM Tb_Despesa WHERE empresa_id = 1;
-- ================================================================

USE SistemaGestaoComercial;
GO

DECLARE @EmpresaId INT = 1;
DECLARE @UsuarioId INT = (SELECT TOP 1 id FROM Tb_Usuario WHERE empresa_id = @EmpresaId AND username = 'Teste');

IF @UsuarioId IS NULL
    SET @UsuarioId = (SELECT TOP 1 id FROM Tb_Usuario WHERE empresa_id = @EmpresaId ORDER BY id);

MERGE Tb_Categoria_Despesa AS destino
USING (VALUES
    ('Aluguel',               'Aluguel do ponto comercial'),
    ('Utensílios Escritório', 'Material de consumo e papelaria'),
    ('Internet e Telefonia',  'Link dedicado e linhas móveis'),
    ('Manutenção',            'Consertos e serviços eventuais')
) AS origem (nome, descricao)
    ON destino.empresa_id = @EmpresaId AND destino.nome = origem.nome
WHEN NOT MATCHED THEN
    INSERT (empresa_id, nome, descricao, ativo)
    VALUES (@EmpresaId, origem.nome, origem.descricao, 1);

DECLARE @Aluguel     INT = (SELECT id FROM Tb_Categoria_Despesa WHERE empresa_id = @EmpresaId AND nome = 'Aluguel');
DECLARE @Utensilios  INT = (SELECT id FROM Tb_Categoria_Despesa WHERE empresa_id = @EmpresaId AND nome = 'Utensílios Escritório');
DECLARE @Internet    INT = (SELECT id FROM Tb_Categoria_Despesa WHERE empresa_id = @EmpresaId AND nome = 'Internet e Telefonia');
DECLARE @Manutencao  INT = (SELECT id FROM Tb_Categoria_Despesa WHERE empresa_id = @EmpresaId AND nome = 'Manutenção');

IF NOT EXISTS (SELECT 1 FROM Tb_Despesa WHERE empresa_id = @EmpresaId)
BEGIN
    INSERT INTO Tb_Despesa (empresa_id, categoria_despesa_id, usuario_id, descricao, valor, data_despesa, fixa)
    VALUES
        (@EmpresaId, @Aluguel,    @UsuarioId, 'Aluguel da loja',                  3200.00, DATEADD(DAY, -18, CAST(GETDATE() AS DATE)), 1),
        (@EmpresaId, @Internet,   @UsuarioId, 'Link dedicado 300MB',               289.90, DATEADD(DAY, -12, CAST(GETDATE() AS DATE)), 1),
        (@EmpresaId, @Utensilios, @UsuarioId, 'Resma de papel e canetas',           147.35, DATEADD(DAY,  -6, CAST(GETDATE() AS DATE)), 0),
        (@EmpresaId, @Manutencao, @UsuarioId, 'Troca do compressor do ar',         1850.00, DATEADD(DAY,  -2, CAST(GETDATE() AS DATE)), 0),
        (@EmpresaId, @Utensilios, @UsuarioId, 'Cartuchos de tinta',                 320.00, CAST(GETDATE() AS DATE),                    0);
END;
GO

SELECT d.id,
       c.nome AS categoria,
       d.valor,
       d.data_despesa,
       d.fixa,
       d.descricao
  FROM Tb_Despesa AS d
  JOIN Tb_Categoria_Despesa AS c ON c.id = d.categoria_despesa_id
 WHERE d.empresa_id = 1
 ORDER BY d.data_despesa DESC;
GO
