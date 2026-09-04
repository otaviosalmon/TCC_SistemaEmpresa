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