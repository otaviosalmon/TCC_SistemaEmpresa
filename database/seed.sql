-- ================================================================
--  SEED — Dados iniciais do sistema
--  Execute APÓS o schema.sql
--  Estes dados são necessários para o sistema funcionar.
-- ================================================================

USE SistemaGestaoComercial;
GO

-- ================================================================
-- Empresa padrão para desenvolvimento
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM Tb_Empresa WHERE cnpj = '00000000000000')
BEGIN
    SET IDENTITY_INSERT Tb_Empresa ON;
    INSERT INTO Tb_Empresa (id, nome, cnpj, cidade, estado, ativo)
    VALUES (1, 'Empresa de Desenvolvimento', '00000000000000', 'São Paulo', 'SP', 1);
    SET IDENTITY_INSERT Tb_Empresa OFF;
END;
GO

-- ================================================================
-- Formas de pagamento
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM Tb_Forma_Pagamento WHERE empresa_id = 1)
BEGIN
    INSERT INTO Tb_Forma_Pagamento (empresa_id, nome, ativo)
    VALUES
        (1, 'Dinheiro',          1),
        (1, 'Cartão Débito',     1),
        (1, 'Cartão Crédito',    1),
        (1, 'Pix',               1),
        (1, 'Boleto',            1),
        (1, 'Transferência',     1);
END;
GO

-- ================================================================
-- Tipos de movimentação de estoque
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM Tb_Tipo_Movimentacao WHERE empresa_id = 1)
BEGIN
    INSERT INTO Tb_Tipo_Movimentacao (empresa_id, nome, natureza, ativo)
    VALUES
        (1, 'Venda',            'SAIDA',   1),
        (1, 'Compra / Entrada', 'ENTRADA', 1),
        (1, 'Ajuste Manual +',  'ENTRADA', 1),
        (1, 'Ajuste Manual -',  'SAIDA',   1),
        (1, 'Perda / Quebra',   'SAIDA',   1),
        (1, 'Devolução',        'ENTRADA', 1);
END;
GO

-- ================================================================
-- Categorias (tipos) de produto
-- Tb_Produto.categoria_produto_id é NOT NULL: sem ao menos uma linha
-- aqui, nenhum produto pode ser cadastrado.
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM Tb_Categoria_Produto WHERE empresa_id = 1)
BEGIN
    INSERT INTO Tb_Categoria_Produto (empresa_id, nome, descricao)
    VALUES
        (1, 'Alimentos', 'Produtos alimentícios em geral'),
        (1, 'Bebidas',   'Bebidas em geral'),
        (1, 'Limpeza',   'Produtos de limpeza'),
        (1, 'Higiene',   'Higiene pessoal'),
        (1, 'Outros',    'Itens sem categoria específica');
END;
GO

-- ================================================================
-- Cargo e usuário admin para primeiro acesso
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM Tb_Cargo WHERE empresa_id = 1 AND nome = 'Administrador')
BEGIN
    INSERT INTO Tb_Cargo (empresa_id, nome, descricao, ativo)
    VALUES (1, 'Administrador', 'Acesso total ao sistema', 1);
END;
GO

-- Usuário admin padrão (senha: Admin@123 — TROQUE antes de ir a produção!)
-- Hash BCrypt de 'Admin@123':
IF NOT EXISTS (SELECT 1 FROM Tb_Usuario WHERE username = 'admin')
BEGIN
    INSERT INTO Tb_Usuario (empresa_id, username, email, password_hash, role, ativo)
    VALUES (1, 'admin', 'admin@empresa.com',
            '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewHlScVUMhCIoTI.',
            'ADMIN', 1);
END;
GO

PRINT 'Seed executado com sucesso!';
GO
