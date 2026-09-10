SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

ALTER TABLE Tb_Cliente DROP CONSTRAINT UQ_Cliente_CPF;
ALTER TABLE Tb_Cliente DROP CONSTRAINT UQ_Cliente_Email;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_Cliente_CPF
    ON Tb_Cliente (empresa_id, cpf)
    WHERE cpf IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_Cliente_Email
    ON Tb_Cliente (empresa_id, email)
    WHERE email IS NOT NULL;
GO


INSERT INTO Tb_Cliente (empresa_id, nome, cpf, data_cadastro, ativo)
VALUES (1, 'Cliente Balcão 1', NULL, GETDATE(), 1);

INSERT INTO Tb_Cliente (empresa_id, nome, cpf, data_cadastro, ativo)
VALUES (1, 'Cliente Balcão 2', NULL, GETDATE(), 1);

SELECT i.name, i.filter_definition
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('Tb_Cliente') AND i.name LIKE 'UQ_Cliente%';

--nova alteração para snapshot de preco_custo

ALTER TABLE Tb_Item_Venda
    ADD preco_custo DECIMAL(10,2) NULL;
GO

ALTER TABLE Tb_Item_Venda
    ADD CONSTRAINT CHK_ItemVenda_PrecoCusto
        CHECK (preco_custo IS NULL OR preco_custo >= 0);
GO

-- ================================================================
-- Username único em TODO o sistema (antes: único por empresa)
-- ================================================================
BEGIN TRY                                                          -- qualquer erro abaixo desvia para o CATCH
    BEGIN TRANSACTION;                                             -- DROP e ADD atômicos: nunca fica sem a constraint

    IF EXISTS (SELECT 1                                            -- trava: existe login repetido?
                 FROM Tb_Usuario
                GROUP BY username                                  -- agrupa com CI_AI ('Admin' = 'admin'), igual à UNIQUE
               HAVING COUNT(*) > 1)                                -- mais de um registro com o mesmo login
    BEGIN
        THROW 50001, 'Há usernames repetidos entre empresas. Renomeie-os antes de migrar.', 1; -- aborta sem alterar nada
    END

    ALTER TABLE Tb_Usuario
        DROP CONSTRAINT UQ_Usuario_Username;                       -- remove a UNIQUE antiga (empresa_id, username)

    ALTER TABLE Tb_Usuario
        ADD CONSTRAINT UQ_Usuario_Username UNIQUE (username);      -- recria com o MESMO nome, só sobre username

    COMMIT TRANSACTION;                                            -- confirma as duas alterações
    PRINT 'UQ_Usuario_Username agora é global.';                   -- feedback no SSMS
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;                       -- desfaz o DROP se algo falhou
    THROW;                                                         -- mostra o erro original
END CATCH;
GO

-- COL_LENGTH devolve NULL quando a coluna não existe: torna o script idempotente
IF COL_LENGTH('Tb_Categoria_Produto', 'ativo') IS NULL
BEGIN
    -- Mesmo tipo e convenção das outras tabelas de cadastro: BIT NOT NULL
    ALTER TABLE Tb_Categoria_Produto
        ADD ativo BIT NOT NULL
            -- DEFAULT nomeado no padrão DF_<Tabela>_<Coluna> (CLAUDE.md §4.0).
            -- Ao adicionar coluna NOT NULL com DEFAULT, o SQL Server preenche as linhas
            -- já existentes com 1: todo tipo cadastrado hoje nasce ATIVO, sem UPDATE manual.
            CONSTRAINT DF_CategoriaProduto_Ativo DEFAULT 1;
END;
GO

-- Conferência: deve listar todos os tipos com ativo = 1
SELECT id, empresa_id, nome, ativo
  FROM Tb_Categoria_Produto;
GO