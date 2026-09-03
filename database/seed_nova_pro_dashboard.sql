-- ================================================================================
--  SEED DE DEMONSTRACAO — L.O. Solutions
--  ------------------------------------------------------------------------------
--  Popula DUAS empresas completas com 12 meses de movimento, para:
--    • testar o isolamento multiempresa (RNF39) — logando em uma e em outra
--    • testar os perfis de acesso (§7) — um usuario de cada role por empresa
--    • dar volume suficiente para os graficos do Dashboard fazerem sentido
--
--  Executar APOS: tcc_database.sql  (schema)
--  Este arquivo e IDEMPOTENTE: pode rodar quantas vezes quiser, ele limpa
--  os proprios dados antes de reinserir. NAO toca na empresa id=1 do seed.sql.
--
--  SENHA DE TODOS OS USUARIOS: Senha@123
-- ================================================================================

USE SistemaGestaoComercial;
GO

SET NOCOUNT ON;
GO

-- ================================================================================
-- 0. PRE-REQUISITOS
--    O seed grava situacao_venda e preco_custo. Se os ALTER TABLE ainda nao
--    foram aplicados, para aqui com mensagem clara em vez de estourar erro solto.
-- ================================================================================

IF COL_LENGTH('Tb_Venda', 'situacao_venda') IS NULL
BEGIN
    RAISERROR('FALTA: ALTER TABLE Tb_Venda ADD situacao_venda. Rode o script de atualizacao antes.', 16, 1);
    SET NOEXEC ON;
END;
GO

IF COL_LENGTH('Tb_Item_Venda', 'preco_custo') IS NULL
BEGIN
    RAISERROR('FALTA: ALTER TABLE Tb_Item_Venda ADD preco_custo. Rode o script de atualizacao antes.', 16, 1);
    SET NOEXEC ON;
END;
GO

PRINT '>> Pre-requisitos OK.';
GO

-- ================================================================================
-- 1. LIMPEZA — remove execucoes anteriores DESTE seed
--    A ordem e a inversa das FKs: filhos primeiro, pais depois.
--    So apaga as duas empresas de demonstracao, identificadas pelo CNPJ.
-- ================================================================================

DECLARE @paraApagar TABLE (id INT PRIMARY KEY);

INSERT INTO @paraApagar (id)
SELECT id FROM Tb_Empresa WHERE cnpj IN ('11222333000181', '44555666000199');

IF EXISTS (SELECT 1 FROM @paraApagar)
BEGIN
    PRINT '>> Limpando dados de execucao anterior...';

    DELETE FROM Tb_Log_Sistema          WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Movimentacao_Estoque WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Item_Venda           WHERE venda_id IN
        (SELECT id FROM Tb_Venda WHERE empresa_id IN (SELECT id FROM @paraApagar));
    DELETE FROM Tb_Venda                WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Despesa              WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Funcionario          WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Produto              WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Cliente              WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Categoria_Produto    WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Categoria_Despesa    WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Forma_Pagamento      WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Tipo_Movimentacao    WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Usuario              WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Cargo                WHERE empresa_id IN (SELECT id FROM @paraApagar);
    DELETE FROM Tb_Empresa              WHERE id IN (SELECT id FROM @paraApagar);
END;
GO

-- ================================================================================
-- 2. EMPRESAS
--    CNPJ sem mascara (CHK_Empresa_CNPJ exige 14 digitos numericos).
-- ================================================================================

DECLARE @emp1 INT, @emp2 INT;

INSERT INTO Tb_Empresa (nome, cnpj, email, endereco, cidade, estado, cep, telefone, ativo)
VALUES ('Mercado Bom Preco LTDA', '11222333000181', 'contato@bompreco.com.br',
        'Rua das Flores, 250', 'Franca', 'SP', '14400000', '1637221100', 1);
SET @emp1 = SCOPE_IDENTITY();

INSERT INTO Tb_Empresa (nome, cnpj, email, endereco, cidade, estado, cep, telefone, ativo)
VALUES ('Tech Store Franca ME', '44555666000199', 'vendas@techstore.com.br',
        'Av. Presidente Vargas, 1820', 'Franca', 'SP', '14405000', '1637335522', 1);
SET @emp2 = SCOPE_IDENTITY();

PRINT '>> Empresas criadas: id ' + CAST(@emp1 AS VARCHAR) + ' e ' + CAST(@emp2 AS VARCHAR);

-- Guarda os ids numa tabela temporaria para os blocos seguintes (variaveis nao
-- sobrevivem ao GO; tabelas #temporarias sim, dentro da mesma sessao).
IF OBJECT_ID('tempdb..#Emp') IS NOT NULL DROP TABLE #Emp;
CREATE TABLE #Emp (ord INT PRIMARY KEY, empresa_id INT NOT NULL, sigla VARCHAR(2) NOT NULL);
INSERT INTO #Emp (ord, empresa_id, sigla) VALUES (1, @emp1, 'bp'), (2, @emp2, 'ts');
GO

-- ================================================================================
-- 3. CARGOS — um por perfil, com salario e comissao base
--    A comissao do cargo e o PADRAO; o funcionario pode sobrescrever (§4.2).
-- ================================================================================

DECLARE @emp1 INT = (SELECT empresa_id FROM #Emp WHERE ord = 1);
DECLARE @emp2 INT = (SELECT empresa_id FROM #Emp WHERE ord = 2);

INSERT INTO Tb_Cargo (empresa_id, nome, descricao, salario_base, per_comissao_base, ativo)
VALUES
    (@emp1, 'Administrador', 'Acesso total ao sistema',            6500.00, NULL, 1),
    (@emp1, 'Gerente',       'Relatorios, dashboards e equipe',    4800.00, 2.00, 1),
    (@emp1, 'Vendedor',      'Registro de vendas no balcao',       2200.00, 5.00, 1),
    (@emp1, 'Operador Caixa','Recebimento e fechamento de caixa',  2000.00, 1.50, 1),
    (@emp1, 'Estoquista',    'Movimentacao e conferencia',         2100.00, NULL, 1),

    (@emp2, 'Administrador', 'Acesso total ao sistema',            7200.00, NULL, 1),
    (@emp2, 'Gerente',       'Relatorios, dashboards e equipe',    5400.00, 2.50, 1),
    (@emp2, 'Vendedor',      'Consultor tecnico de vendas',        2600.00, 6.00, 1),
    (@emp2, 'Operador Caixa','Recebimento e fechamento de caixa',  2200.00, 1.50, 1),
    (@emp2, 'Estoquista',    'Movimentacao e conferencia',         2300.00, NULL, 1);
GO

-- ================================================================================
-- 4. USUARIOS — um de cada role em CADA empresa (10 no total)
--
--    SENHA DE TODOS: Senha@123
--
--    Os hashes abaixo foram gerados com Security/PasswordHasher.cs:
--      PBKDF2-HMAC-SHA256, 210.000 iteracoes, 32 bytes,
--      salt = SHA256("LOSolutions.v1|" + username em minusculas).
--    Formato: PBKDF2-SHA256$<iteracoes>$<hash base64>
--
--    O hash depende do USERNAME: se voce renomear um usuario aqui, a senha
--    dele para de funcionar e o hash precisa ser regerado.
--
--    ATENCAO (pendencia §15 item 4): usernames sao DIFERENTES entre as empresas
--    de proposito. O AccountController atual busca so por username, sem empresa_id;
--    dois homonimos em empresas diferentes fariam o login cair numa empresa
--    arbitraria. Ha um bloco no fim deste arquivo para reproduzir esse bug
--    quando voces forem corrigi-lo.
-- ================================================================================

DECLARE @emp1 INT = (SELECT empresa_id FROM #Emp WHERE ord = 1);
DECLARE @emp2 INT = (SELECT empresa_id FROM #Emp WHERE ord = 2);

INSERT INTO Tb_Usuario (empresa_id, username, email, password_hash, role, ativo, data_cadastro)
VALUES
    -- ---------- Empresa 1: Mercado Bom Preco ----------
    (@emp1, 'admin.bp',      'admin@bompreco.com.br',
     'PBKDF2-SHA256$210000$jH8/BAhX+gVB6gtwZEQgCWHjkhU7rL+pVe4GwBIRuHc=', 'ADMIN',      1, DATEADD(MONTH, -14, GETDATE())),
    (@emp1, 'gerente.bp',    'gerente@bompreco.com.br',
     'PBKDF2-SHA256$210000$Himdb9vbp/l/tPTPtF8SBtQA96zVsaybfATMdTpOEm4=', 'GERENTE',    1, DATEADD(MONTH, -13, GETDATE())),
    (@emp1, 'vendedor.bp',   'vendedor@bompreco.com.br',
     'PBKDF2-SHA256$210000$xBYmvvJ0qwzLXEK7J6+OrN6Ycpa6ZOSC70WvF7sNN8E=', 'VENDEDOR',   1, DATEADD(MONTH, -13, GETDATE())),
    (@emp1, 'caixa.bp',      'caixa@bompreco.com.br',
     'PBKDF2-SHA256$210000$knuPP6pT5LSiCrwnMwgHbmnNtEd/gyO7qvekjOjqn0M=', 'CAIXA',      1, DATEADD(MONTH, -12, GETDATE())),
    (@emp1, 'estoquista.bp', 'estoque@bompreco.com.br',
     'PBKDF2-SHA256$210000$EpgArXIdQbmk2HQhYwVXJeBDuC0cqcYr4OcX1uA8WD4=', 'ESTOQUISTA', 1, DATEADD(MONTH, -12, GETDATE())),

    -- ---------- Empresa 2: Tech Store ----------
    (@emp2, 'admin.ts',      'admin@techstore.com.br',
     'PBKDF2-SHA256$210000$jk0XCH7NOfj4MYviQDe8PXfE9YCI1sgjaAa1Q404X/g=', 'ADMIN',      1, DATEADD(MONTH, -14, GETDATE())),
    (@emp2, 'gerente.ts',    'gerente@techstore.com.br',
     'PBKDF2-SHA256$210000$011I+tTTw/C21xmK4iKWWlqLR206Ypay3BSNjEW6TEU=', 'GERENTE',    1, DATEADD(MONTH, -13, GETDATE())),
    (@emp2, 'vendedor.ts',   'vendedor@techstore.com.br',
     'PBKDF2-SHA256$210000$WPC1/NwXWXBkCNhsnx7shg6SdF0t1VfuuY0Vyjkjl5Q=', 'VENDEDOR',   1, DATEADD(MONTH, -13, GETDATE())),
    (@emp2, 'caixa.ts',      'caixa@techstore.com.br',
     'PBKDF2-SHA256$210000$GIcbvkCeOJuW45WCyTLlddLrgE8bMNyNhnfxTHxXqNE=', 'CAIXA',      1, DATEADD(MONTH, -12, GETDATE())),
    (@emp2, 'estoquista.ts', 'estoque@techstore.com.br',
     'PBKDF2-SHA256$210000$389t5eQMN608nuwxqTDfoiXiCbA7EPodzVdaegW7lN4=', 'ESTOQUISTA', 1, DATEADD(MONTH, -12, GETDATE()));
GO

-- ================================================================================
-- 5. FUNCIONARIOS — cada usuario tem seu funcionario correspondente
--    CPF sem mascara (CHK_Funcionario_CPF: 11 digitos).
--    usuario_id e nullable: o ultimo de cada empresa NAO tem login,
--    para exercitar o caso "funcionario sem acesso ao sistema".
-- ================================================================================

DECLARE @emp1 INT = (SELECT empresa_id FROM #Emp WHERE ord = 1);
DECLARE @emp2 INT = (SELECT empresa_id FROM #Emp WHERE ord = 2);

INSERT INTO Tb_Funcionario
    (empresa_id, usuario_id, cargo_id, nome, cpf, telefone, endereco, salario, per_comissao, data_admissao, ativo)
SELECT @emp1,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp1 AND username = 'admin.bp'),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp1 AND nome = 'Administrador'),
       'Carlos Eduardo Prado', '31122233344', '16991110001', 'Rua Sete de Setembro, 45',
       NULL, NULL, DATEADD(MONTH, -14, CAST(GETDATE() AS DATE)), 1
UNION ALL SELECT @emp1,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp1 AND username = 'gerente.bp'),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp1 AND nome = 'Gerente'),
       'Fernanda Lima Souza', '31233344455', '16991110002', 'Rua General Osorio, 780',
       5100.00, NULL, DATEADD(MONTH, -13, CAST(GETDATE() AS DATE)), 1
UNION ALL SELECT @emp1,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp1 AND username = 'vendedor.bp'),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp1 AND nome = 'Vendedor'),
       'Rafael Moreira Dias', '31344455566', '16991110003', 'Av. Brasil, 1200',
       NULL, 6.50, DATEADD(MONTH, -13, CAST(GETDATE() AS DATE)), 1   -- sobrescreve a comissao do cargo
UNION ALL SELECT @emp1,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp1 AND username = 'caixa.bp'),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp1 AND nome = 'Operador Caixa'),
       'Juliana Alves Rocha', '31455566677', '16991110004', 'Rua do Comercio, 310',
       NULL, NULL, DATEADD(MONTH, -12, CAST(GETDATE() AS DATE)), 1
UNION ALL SELECT @emp1,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp1 AND username = 'estoquista.bp'),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp1 AND nome = 'Estoquista'),
       'Bruno Tavares Nunes', '31566677788', '16991110005', 'Rua Marechal Deodoro, 92',
       NULL, NULL, DATEADD(MONTH, -12, CAST(GETDATE() AS DATE)), 1
UNION ALL SELECT @emp1,
       NULL,   -- funcionario SEM login: usuario_id nullable (§4.2)
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp1 AND nome = 'Estoquista'),
       'Marcos Vinicius Reis', '31677788899', '16991110006', 'Rua Sao Paulo, 55',
       2050.00, NULL, DATEADD(MONTH, -6, CAST(GETDATE() AS DATE)), 1;

INSERT INTO Tb_Funcionario
    (empresa_id, usuario_id, cargo_id, nome, cpf, telefone, endereco, salario, per_comissao, data_admissao, ativo)
SELECT @emp2,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp2 AND username = 'admin.ts'),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp2 AND nome = 'Administrador'),
       'Patricia Gomes Ferraz', '32122233344', '16992220001', 'Av. Rio Negro, 400',
       NULL, NULL, DATEADD(MONTH, -14, CAST(GETDATE() AS DATE)), 1
UNION ALL SELECT @emp2,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp2 AND username = 'gerente.ts'),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp2 AND nome = 'Gerente'),
       'Diego Santana Melo', '32233344455', '16992220002', 'Rua Voluntarios da Franca, 90',
       NULL, NULL, DATEADD(MONTH, -13, CAST(GETDATE() AS DATE)), 1
UNION ALL SELECT @emp2,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp2 AND username = 'vendedor.ts'),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp2 AND nome = 'Vendedor'),
       'Amanda Ribeiro Castro', '32344455566', '16992220003', 'Rua Couto Magalhaes, 610',
       NULL, 7.00, DATEADD(MONTH, -13, CAST(GETDATE() AS DATE)), 1
UNION ALL SELECT @emp2,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp2 AND username = 'caixa.ts'),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp2 AND nome = 'Operador Caixa'),
       'Thiago Pereira Lopes', '32455566677', '16992220004', 'Rua Monsenhor Rosa, 145',
       NULL, NULL, DATEADD(MONTH, -12, CAST(GETDATE() AS DATE)), 1
UNION ALL SELECT @emp2,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp2 AND username = 'estoquista.ts'),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp2 AND nome = 'Estoquista'),
       'Leticia Barbosa Pinto', '32566677788', '16992220005', 'Av. Champagnat, 2200',
       NULL, NULL, DATEADD(MONTH, -12, CAST(GETDATE() AS DATE)), 1
UNION ALL SELECT @emp2,
       NULL,
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp2 AND nome = 'Vendedor'),
       'Gustavo Andrade Cruz', '32677788899', '16992220006', 'Rua Tiradentes, 33',
       NULL, NULL, DATEADD(MONTH, -4, CAST(GETDATE() AS DATE)), 0;   -- INATIVO: testa o filtro dos dropdowns
GO

-- ================================================================================
-- 6. CLIENTES
--    UQ_Cliente_CPF e UQ_Cliente_Email sao (empresa_id, coluna) e a coluna e
--    nullable — no SQL Server, UNIQUE permite APENAS UM NULL. Por isso existe
--    exatamente UM cliente de balcao (cpf e email nulos) por empresa.
--    Um segundo daria violacao de constraint (pendencia §15 item 6).
-- ================================================================================

DECLARE @emp1 INT = (SELECT empresa_id FROM #Emp WHERE ord = 1);
DECLARE @emp2 INT = (SELECT empresa_id FROM #Emp WHERE ord = 2);

INSERT INTO Tb_Cliente (empresa_id, nome, cpf, email, telefone, endereco, data_cadastro, ativo)
VALUES
    (@emp1, 'Ana Paula Ferreira',   '40011122233', 'ana.paula@email.com',  '16993330001', 'Rua Azaleias, 12',   DATEADD(MONTH, -11, GETDATE()), 1),
    (@emp1, 'Roberto Carlos Silva', '40122233344', 'roberto.silva@email.com','16993330002', 'Rua Ipes, 340',    DATEADD(MONTH, -10, GETDATE()), 1),
    (@emp1, 'Mariana Costa Bueno',  '40233344455', 'mariana.bueno@email.com','16993330003', 'Av. Rio Branco, 88',DATEADD(MONTH, -8,  GETDATE()), 1),
    (@emp1, 'Jose Antonio Prado',   '40344455566', 'jose.prado@email.com', '16993330004', 'Rua Bahia, 501',     DATEADD(MONTH, -5,  GETDATE()), 1),
    (@emp1, 'Cliente Balcao',        NULL,          NULL,                   NULL,          NULL,                DATEADD(MONTH, -11, GETDATE()), 1),
    (@emp1, 'Empresa Cliente Antiga','40455566677','antiga@email.com',     '16993330005', 'Rua Goias, 77',      DATEADD(MONTH, -9,  GETDATE()), 0),  -- INATIVO

    (@emp2, 'Lucas Nogueira Pires', '50011122233', 'lucas.pires@email.com','16994440001', 'Rua Parana, 210',   DATEADD(MONTH, -11, GETDATE()), 1),
    (@emp2, 'Camila Duarte Freitas','50122233344', 'camila.freitas@email.com','16994440002','Av. Sao Vicente, 900',DATEADD(MONTH,-9, GETDATE()), 1),
    (@emp2, 'Eduardo Martins Reis', '50233344455', 'eduardo.reis@email.com','16994440003', 'Rua Ceara, 44',     DATEADD(MONTH, -7,  GETDATE()), 1),
    (@emp2, 'Cliente Balcao',        NULL,          NULL,                   NULL,          NULL,               DATEADD(MONTH, -11, GETDATE()), 1);
GO

-- ================================================================================
-- 7. TABELAS DE APOIO — formas de pagamento, tipos de movimentacao, categorias
--    Cada empresa tem as SUAS: sao tabelas com empresa_id, nao globais.
-- ================================================================================

DECLARE @emp1 INT = (SELECT empresa_id FROM #Emp WHERE ord = 1);
DECLARE @emp2 INT = (SELECT empresa_id FROM #Emp WHERE ord = 2);

-- ---------- Formas de pagamento (RF08) ----------
INSERT INTO Tb_Forma_Pagamento (empresa_id, nome, descricao, ativo)
VALUES
    (@emp1, 'Dinheiro',       'Pagamento em especie',            1),
    (@emp1, 'Pix',            'Transferencia instantanea',       1),
    (@emp1, 'Cartao Debito',  'Debito a vista',                  1),
    (@emp1, 'Cartao Credito', 'Credito a vista ou parcelado',    1),
    (@emp1, 'Boleto',         'Boleto bancario',                 1),
    (@emp1, 'Cheque',         'Descontinuado',                   0),  -- INATIVO: testa o filtro
    (@emp2, 'Pix',            'Transferencia instantanea',       1),
    (@emp2, 'Cartao Credito', 'Credito a vista ou parcelado',    1),
    (@emp2, 'Cartao Debito',  'Debito a vista',                  1),
    (@emp2, 'Dinheiro',       'Pagamento em especie',            1),
    (@emp2, 'Boleto',         'Faturado para empresas',          1);

-- ---------- Tipos de movimentacao (RF12) ----------
-- "Ajuste manual" virou DOIS tipos (pendencia §15 item 13): a natureza e fixa
-- por tipo e quantidade e sempre > 0, entao nao da para um tipo unico bidirecional.
INSERT INTO Tb_Tipo_Movimentacao (empresa_id, nome, natureza, descricao, ativo)
VALUES
    (@emp1, 'Baixa por venda',  'SAIDA',   'Gerada automaticamente ao registrar venda', 1),
    (@emp1, 'Compra/Entrada',   'ENTRADA', 'Recebimento de mercadoria do fornecedor',   1),
    (@emp1, 'Devolucao',        'ENTRADA', 'Estorno por cancelamento ou devolucao',     1),
    (@emp1, 'Ajuste de entrada','ENTRADA', 'Correcao de inventario para mais',          1),
    (@emp1, 'Ajuste de saida',  'SAIDA',   'Correcao de inventario para menos',         1),
    (@emp1, 'Perda/Quebra',     'SAIDA',   'Produto avariado ou vencido',               1),
    (@emp2, 'Baixa por venda',  'SAIDA',   'Gerada automaticamente ao registrar venda', 1),
    (@emp2, 'Compra/Entrada',   'ENTRADA', 'Recebimento de mercadoria do fornecedor',   1),
    (@emp2, 'Devolucao',        'ENTRADA', 'Estorno por cancelamento ou devolucao',     1),
    (@emp2, 'Ajuste de entrada','ENTRADA', 'Correcao de inventario para mais',          1),
    (@emp2, 'Ajuste de saida',  'SAIDA',   'Correcao de inventario para menos',         1),
    (@emp2, 'Perda/Quebra',     'SAIDA',   'Produto avariado ou defeituoso',            1);

-- ---------- Categorias de produto ----------
-- Unica tabela de cadastro SEM coluna ativo (pendencia §15 item 9).
INSERT INTO Tb_Categoria_Produto (empresa_id, nome, descricao)
VALUES
    (@emp1, 'Mercearia',  'Alimentos secos e enlatados'),
    (@emp1, 'Bebidas',    'Refrigerantes, sucos e agua'),
    (@emp1, 'Limpeza',    'Produtos de limpeza domestica'),
    (@emp1, 'Higiene',    'Higiene pessoal'),
    (@emp1, 'Hortifruti', 'Frutas, legumes e verduras'),
    (@emp2, 'Perifericos','Teclados, mouses e headsets'),
    (@emp2, 'Componentes','Pecas internas de computador'),
    (@emp2, 'Cabos',      'Cabos e adaptadores'),
    (@emp2, 'Acessorios', 'Suportes, capas e diversos');

-- ---------- Categorias de despesa (RF09) ----------
INSERT INTO Tb_Categoria_Despesa (empresa_id, nome, descricao, ativo)
VALUES
    (@emp1, 'Aluguel',       'Locacao do imovel comercial',   1),
    (@emp1, 'Energia',       'Conta de energia eletrica',     1),
    (@emp1, 'Agua',          'Conta de agua e esgoto',        1),
    (@emp1, 'Internet',      'Link dedicado e telefonia',     1),
    (@emp1, 'Manutencao',    'Reparos e conservacao',         1),
    (@emp1, 'Marketing',     'Divulgacao e publicidade',      1),
    (@emp2, 'Aluguel',       'Locacao da loja',               1),
    (@emp2, 'Energia',       'Conta de energia eletrica',     1),
    (@emp2, 'Internet',      'Link dedicado',                 1),
    (@emp2, 'Marketing',     'Anuncios online',               1),
    (@emp2, 'Manutencao',    'Reparos e conservacao',         1);
GO

-- ================================================================================
-- 8. PRODUTOS
--    quantidade_atual comeca em 0: o estoque e construido no bloco 9, via
--    movimentacoes de ENTRADA. Assim Tb_Produto e Tb_Movimentacao_Estoque
--    ficam coerentes entre si desde o inicio (§6.1), como o sistema exige.
--    CHK_Produto_Preco obriga preco_venda >= preco_custo — margem ~40%.
-- ================================================================================

DECLARE @emp1 INT = (SELECT empresa_id FROM #Emp WHERE ord = 1);
DECLARE @emp2 INT = (SELECT empresa_id FROM #Emp WHERE ord = 2);

INSERT INTO Tb_Produto
    (empresa_id, categoria_produto_id, nome, descricao, preco_custo, preco_venda,
     quantidade_atual, estoque_minimo, data_cadastro, ativo)
SELECT @emp1, c.id, p.nome, NULL, p.custo, p.venda, 0, p.minimo,
       DATEADD(MONTH, -13, GETDATE()), p.ativo
FROM (VALUES
    ('Arroz Tipo 1 5kg',        'Mercearia',  18.90,  29.90, 30, 1),
    ('Feijao Carioca 1kg',      'Mercearia',   5.40,   8.99, 40, 1),
    ('Oleo de Soja 900ml',      'Mercearia',   4.80,   7.49, 50, 1),
    ('Cafe Torrado 500g',       'Mercearia',  11.20,  18.90, 25, 1),
    ('Refrigerante Cola 2L',    'Bebidas',     5.10,   9.49, 60, 1),
    ('Agua Mineral 1,5L',       'Bebidas',     1.40,   2.99, 80, 1),
    ('Suco de Uva Integral 1L', 'Bebidas',     8.60,  14.90, 20, 1),
    ('Detergente Neutro 500ml', 'Limpeza',     1.90,   3.49, 60, 1),
    ('Sabao em Po 1kg',         'Limpeza',     9.30,  15.90, 30, 1),
    ('Papel Higienico 12un',    'Higiene',    14.50,  24.90, 25, 1),
    ('Sabonete 90g',            'Higiene',     1.20,   2.49, 70, 1),
    ('Banana Prata kg',         'Hortifruti',  3.20,   5.99, 15, 1),
    ('Vassoura Descontinuada',  'Limpeza',     7.00,  12.00, 10, 0)   -- INATIVO (RN51)
) AS p(nome, categoria, custo, venda, minimo, ativo)
JOIN Tb_Categoria_Produto c ON c.empresa_id = @emp1 AND c.nome = p.categoria;

INSERT INTO Tb_Produto
    (empresa_id, categoria_produto_id, nome, descricao, preco_custo, preco_venda,
     quantidade_atual, estoque_minimo, data_cadastro, ativo)
SELECT @emp2, c.id, p.nome, NULL, p.custo, p.venda, 0, p.minimo,
       DATEADD(MONTH, -13, GETDATE()), p.ativo
FROM (VALUES
    ('Teclado Mecanico RGB',      'Perifericos', 180.00, 319.90, 8,  1),
    ('Mouse Gamer 7200dpi',       'Perifericos',  75.00, 139.90, 12, 1),
    ('Headset Estereo USB',       'Perifericos', 110.00, 199.90, 10, 1),
    ('Webcam Full HD',            'Perifericos', 125.00, 219.90, 6,  1),
    ('SSD 480GB SATA',            'Componentes', 165.00, 279.90, 10, 1),
    ('Memoria DDR4 8GB',          'Componentes', 130.00, 219.90, 12, 1),
    ('Fonte 500W Bivolt',         'Componentes', 190.00, 329.90, 5,  1),
    ('Cabo HDMI 2m',              'Cabos',        14.00,  29.90, 30, 1),
    ('Cabo USB-C 1m',             'Cabos',         9.50,  22.90, 40, 1),
    ('Suporte para Notebook',     'Acessorios',   42.00,  79.90, 15, 1),
    ('Mousepad Grande',           'Acessorios',   18.00,  39.90, 20, 1),
    ('Adaptador VGA Antigo',      'Cabos',        11.00,  24.90, 5,  0)   -- INATIVO
) AS p(nome, categoria, custo, venda, minimo, ativo)
JOIN Tb_Categoria_Produto c ON c.empresa_id = @emp2 AND c.nome = p.categoria;
GO

-- ================================================================================
-- 9. ENTRADA INICIAL DE ESTOQUE
--    Uma movimentacao de ENTRADA por produto, com quantidade_antes/quantidade_depois
--    preenchidos (o banco permite NULL, mas a §6.1 exige que a aplicacao preencha —
--    o seed segue a mesma regra para nao criar dado que o sistema nunca criaria).
-- ================================================================================

DECLARE @estoqueInicial INT = 1500;

INSERT INTO Tb_Movimentacao_Estoque
    (empresa_id, produto_id, usuario_id, venda_id, tipo_movimentacao_id,
     quantidade, quantidade_antes, quantidade_depois, data_movimentacao, observacao)
SELECT p.empresa_id,
       p.id,
       -- O estoquista da empresa e quem registra a entrada
       (SELECT TOP 1 u.id FROM Tb_Usuario u
         WHERE u.empresa_id = p.empresa_id AND u.role = 'ESTOQUISTA'),
       NULL,                                   -- venda_id NULL: entrada manual, nao veio de venda
       (SELECT TOP 1 t.id FROM Tb_Tipo_Movimentacao t
         WHERE t.empresa_id = p.empresa_id AND t.nome = 'Compra/Entrada'),
       @estoqueInicial,
       0,                                      -- antes: produto entrou zerado no bloco 8
       @estoqueInicial,                        -- depois
       DATEADD(MONTH, -12, GETDATE()),
       'Carga inicial de estoque (seed de demonstracao).'
FROM Tb_Produto p
WHERE p.empresa_id IN (SELECT empresa_id FROM #Emp);

-- Espelha o saldo em Tb_Produto: os dois lugares tem que bater (§6.1)
UPDATE p SET p.quantidade_atual = @estoqueInicial
FROM Tb_Produto p
WHERE p.empresa_id IN (SELECT empresa_id FROM #Emp);
GO

-- ================================================================================
-- 10. DESPESAS — 12 meses de historico
--     Fixas repetem todo mes (insumo do RF34); eventuais aparecem esporadicamente.
--     Os valores foram calibrados para o lucro do dashboard ficar POSITIVO
--     mas nao absurdo, parecido com o mockup da tela.
-- ================================================================================

DECLARE @m INT = 11;
DECLARE @dataRef DATE;
DECLARE @empDesp INT, @usuDesp INT;

WHILE @m >= 0
BEGIN
    -- Dia 5 de cada mes, contando 11 meses atras ate o mes atual
    SET @dataRef = DATEFROMPARTS(YEAR(DATEADD(MONTH, -@m, GETDATE())),
                                 MONTH(DATEADD(MONTH, -@m, GETDATE())), 5);

    DECLARE cur_emp CURSOR LOCAL FAST_FORWARD FOR SELECT empresa_id FROM #Emp;
    OPEN cur_emp;
    FETCH NEXT FROM cur_emp INTO @empDesp;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Gerente da empresa e quem lanca as despesas (usuario_id e NOT NULL)
        SET @usuDesp = (SELECT TOP 1 id FROM Tb_Usuario
                         WHERE empresa_id = @empDesp AND role = 'GERENTE');

        -- ---------- Despesas FIXAS (fixa = 1) ----------
        INSERT INTO Tb_Despesa (empresa_id, categoria_despesa_id, usuario_id,
                                descricao, valor, data_despesa, fixa, observacao)
        SELECT @empDesp, c.id, @usuDesp, d.descricao, d.valor, @dataRef, 1, NULL
        FROM (VALUES
            ('Aluguel',  'Aluguel do imovel',      420.00),
            ('Energia',  'Conta de energia',        130.00),
            ('Internet', 'Link de internet',        89.90)
        ) AS d(categoria, descricao, valor)
        JOIN Tb_Categoria_Despesa c
          ON c.empresa_id = @empDesp AND c.nome = d.categoria;

        -- ---------- Despesas EVENTUAIS (fixa = 0) ----------
        -- Nem todo mes tem: so quando o indice do mes e par ou multiplo de 3.
        IF (@m % 2 = 0)
        BEGIN
            INSERT INTO Tb_Despesa (empresa_id, categoria_despesa_id, usuario_id,
                                    descricao, valor, data_despesa, fixa, observacao)
            SELECT TOP 1 @empDesp, c.id, @usuDesp,
                   'Campanha de divulgacao', 90.00 + (@m * 4), DATEADD(DAY, 9, @dataRef), 0,
                   'Despesa eventual gerada pelo seed.'
            FROM Tb_Categoria_Despesa c
            WHERE c.empresa_id = @empDesp AND c.nome = 'Marketing';
        END;

        IF (@m % 3 = 0)
        BEGIN
            INSERT INTO Tb_Despesa (empresa_id, categoria_despesa_id, usuario_id,
                                    descricao, valor, data_despesa, fixa, observacao)
            SELECT TOP 1 @empDesp, c.id, @usuDesp,
                   'Reparo de equipamento', 110.00 + (@m * 3), DATEADD(DAY, 17, @dataRef), 0,
                   'Despesa eventual gerada pelo seed.'
            FROM Tb_Categoria_Despesa c
            WHERE c.empresa_id = @empDesp AND c.nome = 'Manutencao';
        END;

        FETCH NEXT FROM cur_emp INTO @empDesp;
    END;

    CLOSE cur_emp;
    DEALLOCATE cur_emp;

    SET @m = @m - 1;
END;
GO

-- ================================================================================
-- 11. VENDAS — 12 meses, com itens, baixa de estoque e alguns cancelamentos
--
--     Reproduz EXATAMENTE o fluxo do VendasController.Create (§6.2):
--       1. insere o cabecalho da venda
--       2. insere os itens (sem subtotal — e coluna calculada)
--       3. para cada item, gera a movimentacao "Baixa por venda" com venda_id
--       4. atualiza Tb_Produto.quantidade_atual
--       5. fecha valor_total / valor_final (CHK_Venda_ValorFinal valida a conta)
--
--     A quantidade de vendas CRESCE mes a mes, para o grafico de barras do
--     dashboard mostrar uma curva ascendente como no mockup.
-- ================================================================================

-- Catalogo de apoio: numera produtos, funcionarios, clientes e formas de pagamento
-- de 1..N por empresa, para o loop escolher por modulo (%) sem depender de ids fixos.
IF OBJECT_ID('tempdb..#Prod') IS NOT NULL DROP TABLE #Prod;
SELECT ord = ROW_NUMBER() OVER (PARTITION BY empresa_id ORDER BY id),
       empresa_id, produto_id = id, preco_venda, preco_custo
  INTO #Prod
  FROM Tb_Produto
 WHERE empresa_id IN (SELECT empresa_id FROM #Emp)
   AND ativo = 1;                       -- RN51: produto inativo nao pode ser vendido

IF OBJECT_ID('tempdb..#Func') IS NOT NULL DROP TABLE #Func;
SELECT ord = ROW_NUMBER() OVER (PARTITION BY f.empresa_id ORDER BY f.id),
       f.empresa_id, funcionario_id = f.id
  INTO #Func
  FROM Tb_Funcionario f
  JOIN Tb_Usuario u ON u.id = f.usuario_id
 WHERE f.empresa_id IN (SELECT empresa_id FROM #Emp)
   AND f.ativo = 1
   AND u.role IN ('VENDEDOR', 'CAIXA');  -- quem realmente atende no balcao

IF OBJECT_ID('tempdb..#Cli') IS NOT NULL DROP TABLE #Cli;
SELECT ord = ROW_NUMBER() OVER (PARTITION BY empresa_id ORDER BY id),
       empresa_id, cliente_id = id
  INTO #Cli
  FROM Tb_Cliente
 WHERE empresa_id IN (SELECT empresa_id FROM #Emp) AND ativo = 1;

IF OBJECT_ID('tempdb..#FP') IS NOT NULL DROP TABLE #FP;
SELECT ord = ROW_NUMBER() OVER (PARTITION BY empresa_id ORDER BY id),
       empresa_id, forma_pagamento_id = id
  INTO #FP
  FROM Tb_Forma_Pagamento
 WHERE empresa_id IN (SELECT empresa_id FROM #Emp) AND ativo = 1;
GO

DECLARE @m INT, @v INT, @vendasNoMes INT, @k INT, @itensNaVenda INT;
DECLARE @empV INT, @vendaId INT, @dataVenda DATETIME;
DECLARE @funcId INT, @cliId INT, @fpId INT, @usuOperador INT;
DECLARE @prodId INT, @qtd INT, @precoV DECIMAL(10,2), @precoC DECIMAL(10,2);
DECLARE @qtdAntes INT, @qtdDepois INT;
DECLARE @valorTotal DECIMAL(10,2), @desconto DECIMAL(10,2);
DECLARE @totalProd INT, @totalFunc INT, @totalCli INT, @totalFP INT;
DECLARE @tipoSaida INT, @tipoEntrada INT;
DECLARE @cancelar BIT;

DECLARE cur_empV CURSOR LOCAL FAST_FORWARD FOR SELECT empresa_id FROM #Emp;
OPEN cur_empV;
FETCH NEXT FROM cur_empV INTO @empV;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Quantos itens de cada catalogo esta empresa tem (base do modulo)
    SET @totalProd = (SELECT COUNT(*) FROM #Prod WHERE empresa_id = @empV);
    SET @totalFunc = (SELECT COUNT(*) FROM #Func WHERE empresa_id = @empV);
    SET @totalCli  = (SELECT COUNT(*) FROM #Cli  WHERE empresa_id = @empV);
    SET @totalFP   = (SELECT COUNT(*) FROM #FP   WHERE empresa_id = @empV);

    SET @tipoSaida   = (SELECT TOP 1 id FROM Tb_Tipo_Movimentacao
                         WHERE empresa_id = @empV AND nome = 'Baixa por venda');
    SET @tipoEntrada = (SELECT TOP 1 id FROM Tb_Tipo_Movimentacao
                         WHERE empresa_id = @empV AND nome = 'Devolucao');
    SET @usuOperador = (SELECT TOP 1 id FROM Tb_Usuario
                         WHERE empresa_id = @empV AND role = 'VENDEDOR');

    SET @m = 11;
    WHILE @m >= 0
    BEGIN
        -- Crescimento: 4 vendas no mes mais antigo, ate 15 no mes atual
        SET @vendasNoMes = 4 + (11 - @m);

        SET @v = 1;
        WHILE @v <= @vendasNoMes
        BEGIN
            -- Espalha as vendas ao longo do mes (dia 1..28) e do dia (9h..18h)
            SET @dataVenda = DATEADD(HOUR, 9 + (@v % 9),
                             CAST(DATEFROMPARTS(
                                 YEAR (DATEADD(MONTH, -@m, GETDATE())),
                                 MONTH(DATEADD(MONTH, -@m, GETDATE())),
                                 1 + ((@v * 2) % 28)) AS DATETIME));

            -- Rotaciona funcionario, cliente e forma de pagamento
            SET @funcId = (SELECT funcionario_id FROM #Func
                            WHERE empresa_id = @empV AND ord = (@v % @totalFunc) + 1);
            SET @cliId  = (SELECT cliente_id FROM #Cli
                            WHERE empresa_id = @empV AND ord = ((@v + @m) % @totalCli) + 1);
            SET @fpId   = (SELECT forma_pagamento_id FROM #FP
                            WHERE empresa_id = @empV AND ord = ((@v + @m * 2) % @totalFP) + 1);

            -- A cada 5 vendas, uma e de balcao: cliente_id NULL (coluna e nullable)
            IF (@v % 5 = 0) SET @cliId = NULL;

            -- ---------- Cabecalho ----------
            -- Entra zerado: 0 = 0 - 0 satisfaz CHK_Venda_ValorFinal.
            -- Os valores reais sao gravados no UPDATE depois dos itens.
            INSERT INTO Tb_Venda (empresa_id, funcionario_id, cliente_id, forma_pagamento_id,
                                  data_venda, valor_total, desconto, valor_final,
                                  observacao, situacao_venda)
            VALUES (@empV, @funcId, @cliId, @fpId, @dataVenda, 0, 0, 0,
                    NULL, 'CONCLUIDA');

            SET @vendaId = SCOPE_IDENTITY();
            SET @valorTotal = 0;

            -- ---------- Itens: 1 a 3 produtos por venda ----------
            SET @itensNaVenda = 2 + ((@v + @m) % 3);
            SET @k = 0;

            WHILE @k < @itensNaVenda
            BEGIN
                -- Escolhe o produto por modulo. Os multiplicadores primos (7 e 3)
                -- espalham a selecao para o "top 5" do grafico ficar variado.
                SELECT @prodId = produto_id, @precoV = preco_venda, @precoC = preco_custo
                  FROM #Prod
                 WHERE empresa_id = @empV
                   AND ord = (((@v * 7) + (@k * 3) + @m) % @totalProd) + 1;

                -- O controller proibe o mesmo produto duas vezes na mesma venda.
                -- Se o modulo repetiu, pula este item em vez de gravar duplicado.
                IF NOT EXISTS (SELECT 1 FROM Tb_Item_Venda
                                WHERE venda_id = @vendaId AND produto_id = @prodId)
                BEGIN
                    SET @qtd = 3 + ((@prodId + @v + @k) % 4);   -- 1 a 4 unidades

                    -- Saldo antes/depois, lidos do produto na hora (§6.1)
                    SET @qtdAntes  = (SELECT quantidade_atual FROM Tb_Produto WHERE id = @prodId);
                    SET @qtdDepois = @qtdAntes - @qtd;

                    -- CHK_Produto_QtdAtual proibe negativo: se faltar estoque, pula.
                    IF @qtdDepois >= 0
                    BEGIN
                        -- subtotal NAO e informado: e COMPUTED PERSISTED no banco
                        INSERT INTO Tb_Item_Venda (venda_id, produto_id, quantidade,
                                                   preco_unitario, preco_custo)
                        VALUES (@vendaId, @prodId, @qtd, @precoV, @precoC);
                        --                                ^^^^^^^ snapshot do custo (RF24)

                        INSERT INTO Tb_Movimentacao_Estoque
                            (empresa_id, produto_id, usuario_id, venda_id, tipo_movimentacao_id,
                             quantidade, quantidade_antes, quantidade_depois,
                             data_movimentacao, observacao)
                        VALUES (@empV, @prodId, @usuOperador, @vendaId, @tipoSaida,
                                @qtd, @qtdAntes, @qtdDepois,
                                @dataVenda, 'Baixa automatica pela venda.');

                        UPDATE Tb_Produto SET quantidade_atual = @qtdDepois WHERE id = @prodId;

                        SET @valorTotal = @valorTotal + (@precoV * @qtd);
                    END;
                END;

                SET @k = @k + 1;
            END;

            -- ---------- Fechamento do cabecalho ----------
            IF @valorTotal > 0
            BEGIN
                -- Desconto de 5% a cada 4 vendas; nunca maior que o total
                SET @desconto = CASE WHEN @v % 4 = 0
                                     THEN ROUND(@valorTotal * 0.05, 2)
                                     ELSE 0 END;

                UPDATE Tb_Venda
                   SET valor_total = @valorTotal,
                       desconto    = @desconto,
                       valor_final = @valorTotal - @desconto   -- CHK_Venda_ValorFinal
                 WHERE id = @vendaId;

                -- ---------- Cancelamento (RF18) ----------
                -- Duas vendas por empresa nascem canceladas, para o filtro da
                -- listagem e a exclusao do dashboard terem o que testar.
                SET @cancelar = CASE WHEN @m IN (7, 3) AND @v = 2 THEN 1 ELSE 0 END;

                IF @cancelar = 1
                BEGIN
                    -- Estorno: NAO apaga as movimentacoes originais (isso destruiria
                    -- a rastreabilidade). Gera movimentacoes de ENTRADA opostas.
                    DECLARE @itemProd INT, @itemQtd INT;

                    DECLARE cur_estorno CURSOR LOCAL FAST_FORWARD FOR
                        SELECT produto_id, quantidade FROM Tb_Item_Venda WHERE venda_id = @vendaId;
                    OPEN cur_estorno;
                    FETCH NEXT FROM cur_estorno INTO @itemProd, @itemQtd;

                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        SET @qtdAntes  = (SELECT quantidade_atual FROM Tb_Produto WHERE id = @itemProd);
                        SET @qtdDepois = @qtdAntes + @itemQtd;

                        INSERT INTO Tb_Movimentacao_Estoque
                            (empresa_id, produto_id, usuario_id, venda_id, tipo_movimentacao_id,
                             quantidade, quantidade_antes, quantidade_depois,
                             data_movimentacao, observacao)
                        VALUES (@empV, @itemProd, @usuOperador, @vendaId, @tipoEntrada,
                                @itemQtd, @qtdAntes, @qtdDepois,
                                DATEADD(DAY, 1, @dataVenda),
                                'Estorno de estoque pelo cancelamento da venda: ' + CAST(@vendaId AS VARCHAR));

                        UPDATE Tb_Produto SET quantidade_atual = @qtdDepois WHERE id = @itemProd;

                        FETCH NEXT FROM cur_estorno INTO @itemProd, @itemQtd;
                    END;

                    CLOSE cur_estorno;
                    DEALLOCATE cur_estorno;

                    UPDATE Tb_Venda SET situacao_venda = 'CANCELADA' WHERE id = @vendaId;

                    -- RN52: cancelamento e operacao critica, gera log
                    INSERT INTO Tb_Log_Sistema (empresa_id, usuario_id, acao, entidade_afetada,
                                                registro_id, data_hora, detalhes)
                    VALUES (@empV,
                            (SELECT TOP 1 id FROM Tb_Usuario WHERE empresa_id = @empV AND role = 'ADMIN'),
                            'CANCELAMENTO', 'Venda', @vendaId, DATEADD(DAY, 1, @dataVenda),
                            'Venda cancelada e estoque estornado (seed de demonstracao).');
                END
                ELSE
                BEGIN
                    -- Log de criacao apenas em parte das vendas, para nao inflar a tabela
                    IF @v % 3 = 0
                    INSERT INTO Tb_Log_Sistema (empresa_id, usuario_id, acao, entidade_afetada,
                                                registro_id, data_hora, detalhes)
                    VALUES (@empV, @usuOperador, 'CRIACAO', 'Venda', @vendaId, @dataVenda,
                            'Venda registrada pelo seed de demonstracao.');
                END;
            END
            ELSE
            BEGIN
                -- Nenhum item entrou (estoque esgotado): remove o cabecalho orfao,
                -- porque a RN46 exige ao menos um item por venda.
                DELETE FROM Tb_Venda WHERE id = @vendaId;
            END;

            SET @v = @v + 1;
        END;

        SET @m = @m - 1;
    END;

    FETCH NEXT FROM cur_empV INTO @empV;
END;

CLOSE cur_empV;
DEALLOCATE cur_empV;
GO

-- ================================================================================
-- 12. AJUSTES E PERDAS — movimentacoes SEM venda (venda_id NULL)
--     Exercitam a tela de Movimentacoes e a distincao manual x automatica.
-- ================================================================================

DECLARE @empA INT, @prodA INT, @usuA INT, @tipoA INT, @antes INT, @depois INT;

DECLARE cur_ajuste CURSOR LOCAL FAST_FORWARD FOR
    SELECT TOP 6 p.empresa_id, p.id
      FROM Tb_Produto p
     WHERE p.empresa_id IN (SELECT empresa_id FROM #Emp) AND p.ativo = 1
     ORDER BY p.empresa_id, p.id;

OPEN cur_ajuste;
FETCH NEXT FROM cur_ajuste INTO @empA, @prodA;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @usuA  = (SELECT TOP 1 id FROM Tb_Usuario WHERE empresa_id = @empA AND role = 'ESTOQUISTA');
    SET @tipoA = (SELECT TOP 1 id FROM Tb_Tipo_Movimentacao WHERE empresa_id = @empA AND nome = 'Perda/Quebra');
    SET @antes  = (SELECT quantidade_atual FROM Tb_Produto WHERE id = @prodA);
    SET @depois = CASE WHEN @antes >= 3 THEN @antes - 3 ELSE @antes END;

    IF @depois <> @antes
    BEGIN
        INSERT INTO Tb_Movimentacao_Estoque
            (empresa_id, produto_id, usuario_id, venda_id, tipo_movimentacao_id,
             quantidade, quantidade_antes, quantidade_depois, data_movimentacao, observacao)
        VALUES (@empA, @prodA, @usuA, NULL, @tipoA,      -- venda_id NULL: movimentacao manual
                3, @antes, @depois, DATEADD(DAY, -20, GETDATE()),
                'Produto avariado durante conferencia.');

        UPDATE Tb_Produto SET quantidade_atual = @depois WHERE id = @prodA;
    END;

    FETCH NEXT FROM cur_ajuste INTO @empA, @prodA;
END;

CLOSE cur_ajuste;
DEALLOCATE cur_ajuste;
GO

-- ================================================================================
-- 13. ESTOQUE BAIXO — força alguns produtos abaixo do minimo
--     Para a view Vw_Produtos_Abaixo_Estoque_Minimo (RF23) ter o que retornar.
--     Lembrete: a view usa "<" estrito, entao o produto tem que ficar ABAIXO,
--     nao apenas igual ao minimo (pendencia §15 item 16).
-- ================================================================================

UPDATE p
   SET p.quantidade_atual = CASE WHEN p.estoque_minimo >= 2
                                 THEN p.estoque_minimo - 2
                                 ELSE 0 END
  FROM Tb_Produto p
  JOIN (SELECT empresa_id, id,
               ord = ROW_NUMBER() OVER (PARTITION BY empresa_id ORDER BY id)
          FROM Tb_Produto
         WHERE empresa_id IN (SELECT empresa_id FROM #Emp) AND ativo = 1) AS alvo
    ON alvo.id = p.id
 WHERE alvo.ord IN (2, 5);   -- dois produtos por empresa ficam em alerta
GO

-- ================================================================================
-- 14. LOGS ADICIONAIS — cobre outras acoes alem de venda
-- ================================================================================

INSERT INTO Tb_Log_Sistema (empresa_id, usuario_id, acao, entidade_afetada,
                            registro_id, data_hora, detalhes)
SELECT e.empresa_id,
       (SELECT TOP 1 u.id FROM Tb_Usuario u WHERE u.empresa_id = e.empresa_id AND u.role = 'ADMIN'),
       l.acao, l.entidade, l.registro, DATEADD(DAY, -l.diasAtras, GETDATE()), l.detalhes
FROM #Emp e
CROSS JOIN (VALUES
    ('CRIACAO',  'Usuario',     1, 60, 'Usuario criado durante a implantacao.'),
    ('CRIACAO',  'Produto',     1, 58, 'Cadastro inicial de produtos importado.'),
    ('ALTERACAO','Produto',     3, 30, 'Preco de venda reajustado.'),
    ('EXCLUSAO', 'Cliente',     6, 25, 'Cliente inativado a pedido do titular.'),
    ('ALTERACAO','Funcionario', 6, 12, 'Funcionario inativado por desligamento.')
) AS l(acao, entidade, registro, diasAtras, detalhes);
GO

-- ================================================================================
-- 15. LIMPEZA DAS TABELAS TEMPORARIAS
-- ================================================================================

IF OBJECT_ID('tempdb..#Prod') IS NOT NULL DROP TABLE #Prod;
IF OBJECT_ID('tempdb..#Func') IS NOT NULL DROP TABLE #Func;
IF OBJECT_ID('tempdb..#Cli')  IS NOT NULL DROP TABLE #Cli;
IF OBJECT_ID('tempdb..#FP')   IS NOT NULL DROP TABLE #FP;
GO

-- ================================================================================
-- 16. RESUMO — confira se os numeros bateram
-- ================================================================================

PRINT '';
PRINT '================================================================';
PRINT ' SEED DE DEMONSTRACAO CONCLUIDO';
PRINT '================================================================';
GO

SELECT  Empresa          = e.nome,
        Usuarios         = (SELECT COUNT(*) FROM Tb_Usuario     WHERE empresa_id = e.id),
        Funcionarios     = (SELECT COUNT(*) FROM Tb_Funcionario WHERE empresa_id = e.id),
        Clientes         = (SELECT COUNT(*) FROM Tb_Cliente     WHERE empresa_id = e.id),
        Produtos         = (SELECT COUNT(*) FROM Tb_Produto     WHERE empresa_id = e.id),
        Vendas           = (SELECT COUNT(*) FROM Tb_Venda       WHERE empresa_id = e.id),
        Canceladas       = (SELECT COUNT(*) FROM Tb_Venda       WHERE empresa_id = e.id AND situacao_venda = 'CANCELADA'),
        Movimentacoes    = (SELECT COUNT(*) FROM Tb_Movimentacao_Estoque WHERE empresa_id = e.id),
        Despesas         = (SELECT COUNT(*) FROM Tb_Despesa     WHERE empresa_id = e.id)
  FROM Tb_Empresa e
 WHERE e.cnpj IN ('11222333000181', '44555666000199');
GO

-- KPIs do dashboard, calculados em SQL — devem bater com a tela
SELECT  Empresa        = e.nome,
        ReceitaBruta   = ISNULL((SELECT SUM(v.valor_final) FROM Tb_Venda v
                                  WHERE v.empresa_id = e.id AND v.situacao_venda = 'CONCLUIDA'), 0),
        CMV            = ISNULL((SELECT SUM(ISNULL(iv.preco_custo, p.preco_custo) * iv.quantidade)
                                   FROM Tb_Item_Venda iv
                                   JOIN Tb_Venda   v ON v.id = iv.venda_id
                                   JOIN Tb_Produto p ON p.id = iv.produto_id
                                  WHERE v.empresa_id = e.id AND v.situacao_venda = 'CONCLUIDA'), 0),
        ReceitaLiquida = ISNULL((SELECT SUM(v.valor_final) FROM Tb_Venda v
                                  WHERE v.empresa_id = e.id AND v.situacao_venda = 'CONCLUIDA'), 0)
                       - ISNULL((SELECT SUM(ISNULL(iv.preco_custo, p.preco_custo) * iv.quantidade)
                                   FROM Tb_Item_Venda iv
                                   JOIN Tb_Venda   v ON v.id = iv.venda_id
                                   JOIN Tb_Produto p ON p.id = iv.produto_id
                                  WHERE v.empresa_id = e.id AND v.situacao_venda = 'CONCLUIDA'), 0),
        TotalDespesas  = ISNULL((SELECT SUM(d.valor) FROM Tb_Despesa d WHERE d.empresa_id = e.id), 0)
  FROM Tb_Empresa e
 WHERE e.cnpj IN ('11222333000181', '44555666000199');
GO

-- Produtos em alerta de estoque minimo (RF23)
SELECT * FROM Vw_Produtos_Abaixo_Estoque_Minimo;
GO

PRINT '';
PRINT 'CREDENCIAIS (senha de todos: Senha@123)';
PRINT '  Mercado Bom Preco : admin.bp | gerente.bp | vendedor.bp | caixa.bp | estoquista.bp';
PRINT '  Tech Store Franca : admin.ts | gerente.ts | vendedor.ts | caixa.ts | estoquista.ts';
GO

SET NOEXEC OFF;
GO

-- ================================================================================
-- APENDICE (opcional) — reproduzir a pendencia §15 item 4
--
-- O AccountController atual busca o usuario SO por username, sem empresa_id.
-- UQ_Usuario_Username e (empresa_id, username), entao dois homonimos em empresas
-- diferentes sao permitidos pelo banco — e o login cai numa empresa arbitraria,
-- furando o RNF39. Descomente para reproduzir o bug e validar a correcao.
--
-- INSERT INTO Tb_Usuario (empresa_id, username, email, password_hash, role, ativo)
-- SELECT id, 'gerente', 'gerente.dup@bompreco.com.br',
--        'PBKDF2-SHA256$210000$COLOQUE_O_HASH_AQUI=', 'GERENTE', 1
--   FROM Tb_Empresa WHERE cnpj = '11222333000181';
--
-- INSERT INTO Tb_Usuario (empresa_id, username, email, password_hash, role, ativo)
-- SELECT id, 'gerente', 'gerente.dup@techstore.com.br',
--        'PBKDF2-SHA256$210000$COLOQUE_O_HASH_AQUI=', 'GERENTE', 1
--   FROM Tb_Empresa WHERE cnpj = '44555666000199';
-- ================================================================================
