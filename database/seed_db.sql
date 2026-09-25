USE SistemaGestaoComercial;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
BEGIN TRANSACTION;

DECLARE @cnpj1 VARCHAR(14) = '11222333000181';
DECLARE @cnpj2 VARCHAR(14) = '44555666000199';

DECLARE @alvo TABLE (id INT PRIMARY KEY);
INSERT INTO @alvo (id) SELECT id FROM Tb_Empresa WHERE cnpj IN (@cnpj1, @cnpj2);

DELETE FROM Tb_Log_Sistema          WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Movimentacao_Estoque WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Item_Venda           WHERE venda_id IN (SELECT id FROM Tb_Venda WHERE empresa_id IN (SELECT id FROM @alvo));
DELETE FROM Tb_Venda                WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Despesa              WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Produto              WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Categoria_Produto    WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Funcionario          WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Usuario              WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Cargo                WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Cliente              WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Forma_Pagamento      WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Tipo_Movimentacao    WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Categoria_Despesa    WHERE empresa_id IN (SELECT id FROM @alvo);
DELETE FROM Tb_Empresa              WHERE id IN (SELECT id FROM @alvo);

DECLARE @emp1 INT, @emp2 INT;

INSERT INTO Tb_Empresa (nome, cnpj, email, endereco, cidade, estado, cep, telefone, ativo)
VALUES ('Mercado Bom Preco LTDA', @cnpj1, 'contato@bompreco.com.br',
        'Rua das Flores, 250', 'Franca', 'SP', '14400000', '1637221100', 1);
SET @emp1 = SCOPE_IDENTITY();

INSERT INTO Tb_Empresa (nome, cnpj, email, endereco, cidade, estado, cep, telefone, ativo)
VALUES ('Tech Store Franca ME', @cnpj2, 'vendas@techstore.com.br',
        'Av. Presidente Vargas, 1820', 'Franca', 'SP', '14405000', '1637335522', 1);
SET @emp2 = SCOPE_IDENTITY();

DECLARE @mesBase  DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
DECLARE @inicio   DATE = '20240101';
DECLARE @meses    INT  = DATEDIFF(MONTH, @inicio, @mesBase);

INSERT INTO Tb_Forma_Pagamento (empresa_id, nome, descricao, ativo)
SELECT e.id, f.nome, f.descricao, f.ativo
FROM (VALUES (@emp1), (@emp2)) AS e(id)
CROSS JOIN (VALUES
    ('Dinheiro',        'Pagamento em especie',            1),
    ('Cartao Debito',   'Debito a vista',                  1),
    ('Cartao Credito',  'Credito a vista ou parcelado',    1),
    ('Pix',             'Transferencia instantanea',       1),
    ('Boleto',          'Boleto bancario',                 1),
    ('Transferencia',   'TED ou DOC',                      0)
) AS f(nome, descricao, ativo);

INSERT INTO Tb_Tipo_Movimentacao (empresa_id, nome, natureza, descricao, ativo)
SELECT e.id, t.nome, t.natureza, t.descricao, 1
FROM (VALUES (@emp1), (@emp2)) AS e(id)
CROSS JOIN (VALUES
    ('Baixa por venda',     'SAIDA',   'Gerada automaticamente na confirmacao da venda'),
    ('Compra de fornecedor','ENTRADA', 'Reposicao de estoque'),
    ('Devolucao',           'ENTRADA', 'Retorno de mercadoria ou estorno de cancelamento'),
    ('Ajuste de entrada',   'ENTRADA', 'Correcao de inventario para mais'),
    ('Ajuste de saida',     'SAIDA',   'Correcao de inventario para menos'),
    ('Perda ou quebra',     'SAIDA',   'Baixa por avaria')
) AS t(nome, natureza, descricao);

INSERT INTO Tb_Categoria_Despesa (empresa_id, nome, descricao, ativo)
SELECT e.id, c.nome, c.descricao, 1
FROM (VALUES (@emp1), (@emp2)) AS e(id)
CROSS JOIN (VALUES
    ('Aluguel',             'Locacao do imovel comercial'),
    ('Folha de Pagamento',  'Salarios e encargos'),
    ('Utilidades',          'Energia, agua e internet'),
    ('Impostos e Taxas',    'Simples Nacional')
) AS c(nome, descricao);

INSERT INTO Tb_Categoria_Produto (empresa_id, nome, descricao, ativo)
VALUES
    (@emp1, 'Mercearia',    'Secos e molhados',                 1),
    (@emp1, 'Bebidas',      'Bebidas em geral',                 1),
    (@emp1, 'Limpeza',      'Produtos de limpeza domestica',    1),
    (@emp1, 'Higiene',      'Higiene pessoal',                  1),
    (@emp1, 'Hortifruti',   'Frutas, legumes e verduras',       1),
    (@emp2, 'Perifericos',  'Teclados, mouses e headsets',      1),
    (@emp2, 'Componentes',  'Pecas internas de computador',     1),
    (@emp2, 'Cabos',        'Cabos e adaptadores',              1),
    (@emp2, 'Redes',        'Equipamentos de rede',             1);

INSERT INTO Tb_Cargo (empresa_id, nome, descricao, salario_base, per_comissao_base, ativo)
VALUES
    (@emp1, 'Administrador',  'Acesso total ao sistema',           6500.00, NULL, 1),
    (@emp1, 'Gerente',        'Relatorios, dashboards e equipe',   4800.00, 2.00, 1),
    (@emp1, 'Vendedor',       'Registro de vendas no balcao',      2200.00, 5.00, 1),
    (@emp1, 'Operador Caixa', 'Recebimento e fechamento de caixa', 2000.00, 1.50, 1),
    (@emp1, 'Estoquista',     'Movimentacao e conferencia',        2100.00, NULL, 1),
    (@emp2, 'Administrador',  'Acesso total ao sistema',           7200.00, NULL, 1),
    (@emp2, 'Gerente',        'Relatorios, dashboards e equipe',   5400.00, 2.50, 1),
    (@emp2, 'Vendedor',       'Consultor tecnico de vendas',       2600.00, 6.00, 1),
    (@emp2, 'Operador Caixa', 'Recebimento e fechamento de caixa', 2200.00, 1.50, 1),
    (@emp2, 'Estoquista',     'Movimentacao e conferencia',        2300.00, NULL, 1);

INSERT INTO Tb_Usuario (empresa_id, username, email, password_hash, role, ativo, data_cadastro)
VALUES
    (@emp1, 'admin.bp',      'admin@bompreco.com.br',
     'PBKDF2-SHA256$210000$jH8/BAhX+gVB6gtwZEQgCWHjkhU7rL+pVe4GwBIRuHc=', 'ADMIN',      1, @inicio),
    (@emp1, 'gerente.bp',    'gerente@bompreco.com.br',
     'PBKDF2-SHA256$210000$Himdb9vbp/l/tPTPtF8SBtQA96zVsaybfATMdTpOEm4=', 'GERENTE',    1, @inicio),
    (@emp1, 'vendedor.bp',   'vendedor@bompreco.com.br',
     'PBKDF2-SHA256$210000$xBYmvvJ0qwzLXEK7J6+OrN6Ycpa6ZOSC70WvF7sNN8E=', 'VENDEDOR',   1, @inicio),
    (@emp1, 'caixa.bp',      'caixa@bompreco.com.br',
     'PBKDF2-SHA256$210000$knuPP6pT5LSiCrwnMwgHbmnNtEd/gyO7qvekjOjqn0M=', 'CAIXA',      1, @inicio),
    (@emp1, 'estoquista.bp', 'estoque@bompreco.com.br',
     'PBKDF2-SHA256$210000$EpgArXIdQbmk2HQhYwVXJeBDuC0cqcYr4OcX1uA8WD4=', 'ESTOQUISTA', 1, @inicio),
    (@emp2, 'admin.ts',      'admin@techstore.com.br',
     'PBKDF2-SHA256$210000$jk0XCH7NOfj4MYviQDe8PXfE9YCI1sgjaAa1Q404X/g=', 'ADMIN',      1, @inicio),
    (@emp2, 'gerente.ts',    'gerente@techstore.com.br',
     'PBKDF2-SHA256$210000$011I+tTTw/C21xmK4iKWWlqLR206Ypay3BSNjEW6TEU=', 'GERENTE',    1, @inicio),
    (@emp2, 'vendedor.ts',   'vendedor@techstore.com.br',
     'PBKDF2-SHA256$210000$WPC1/NwXWXBkCNhsnx7shg6SdF0t1VfuuY0Vyjkjl5Q=', 'VENDEDOR',   1, @inicio),
    (@emp2, 'caixa.ts',      'caixa@techstore.com.br',
     'PBKDF2-SHA256$210000$GIcbvkCeOJuW45WCyTLlddLrgE8bMNyNhnfxTHxXqNE=', 'CAIXA',      1, @inicio),
    (@emp2, 'estoquista.ts', 'estoque@techstore.com.br',
     'PBKDF2-SHA256$210000$389t5eQMN608nuwxqTDfoiXiCbA7EPodzVdaegW7lN4=', 'ESTOQUISTA', 1, @inicio);

INSERT INTO Tb_Funcionario
    (empresa_id, usuario_id, cargo_id, nome, cpf, telefone, endereco, salario, per_comissao, data_admissao, ativo)
SELECT @emp1,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp1 AND username = f.login),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp1 AND nome = f.cargo),
       f.nome, f.cpf, f.telefone, f.endereco, f.salario, f.comissao, @inicio, 1
FROM (VALUES
    ('admin.bp',      'Administrador',  'Carlos Eduardo Prado',  '31122233344', '16991110001', 'Rua Sete de Setembro, 45',   NULL,    NULL),
    ('gerente.bp',    'Gerente',        'Fernanda Lima Souza',   '31233344455', '16991110002', 'Rua General Osorio, 780',    5100.00, NULL),
    ('vendedor.bp',   'Vendedor',       'Rafael Moreira Dias',   '31344455566', '16991110003', 'Av. Brasil, 1200',           NULL,    6.50),
    ('caixa.bp',      'Operador Caixa', 'Juliana Alves Rocha',   '31455566677', '16991110004', 'Rua do Comercio, 310',       NULL,    NULL),
    ('estoquista.bp', 'Estoquista',     'Bruno Tavares Nunes',   '31566677788', '16991110005', 'Rua Marechal Deodoro, 92',   NULL,    NULL)
) AS f(login, cargo, nome, cpf, telefone, endereco, salario, comissao);

INSERT INTO Tb_Funcionario
    (empresa_id, usuario_id, cargo_id, nome, cpf, telefone, endereco, salario, per_comissao, data_admissao, ativo)
VALUES (@emp1, NULL,
        (SELECT id FROM Tb_Cargo WHERE empresa_id = @emp1 AND nome = 'Estoquista'),
        'Marcos Vinicius Reis', '31677788899', '16991110006', 'Rua Sao Paulo, 55',
        2050.00, NULL, @inicio, 1);

INSERT INTO Tb_Funcionario
    (empresa_id, usuario_id, cargo_id, nome, cpf, telefone, endereco, salario, per_comissao, data_admissao, ativo)
SELECT @emp2,
       (SELECT id FROM Tb_Usuario WHERE empresa_id = @emp2 AND username = f.login),
       (SELECT id FROM Tb_Cargo   WHERE empresa_id = @emp2 AND nome = f.cargo),
       f.nome, f.cpf, f.telefone, f.endereco, f.salario, f.comissao, @inicio, 1
FROM (VALUES
    ('admin.ts',      'Administrador',  'Patricia Gomes Ferraz',  '32122233344', '16992220001', 'Av. Rio Negro, 400',              NULL,    NULL),
    ('gerente.ts',    'Gerente',        'Diego Santana Melo',     '32233344455', '16992220002', 'Rua Voluntarios da Franca, 90',   NULL,    NULL),
    ('vendedor.ts',   'Vendedor',       'Amanda Ribeiro Castro',  '32344455566', '16992220003', 'Rua Couto Magalhaes, 610',        NULL,    7.00),
    ('caixa.ts',      'Operador Caixa', 'Thiago Pereira Lopes',   '32455566677', '16992220004', 'Rua Monsenhor Rosa, 145',         NULL,    NULL),
    ('estoquista.ts', 'Estoquista',     'Leticia Barbosa Pinto',  '32566677788', '16992220005', 'Av. Champagnat, 2200',            NULL,    NULL)
) AS f(login, cargo, nome, cpf, telefone, endereco, salario, comissao);

INSERT INTO Tb_Cliente (empresa_id, nome, cpf, email, telefone, endereco, data_cadastro, ativo)
VALUES
    (@emp1, 'Ana Paula Ferreira',    '41122233344', 'ana.ferreira@email.com',    '16993330001', 'Rua Tiradentes, 120',      @inicio, 1),
    (@emp1, 'Joao Batista Nogueira', '41233344455', 'joao.nogueira@email.com',   '16993330002', 'Rua Santa Cruz, 340',      @inicio, 1),
    (@emp1, 'Marcia Regina Alves',   '41344455566', 'marcia.alves@email.com',    '16993330003', 'Av. Dr. Flavio Rocha, 88', @inicio, 1),
    (@emp1, 'Roberto Carlos Pinto',  '41455566677', 'roberto.pinto@email.com',   '16993330004', 'Rua Bahia, 512',           @inicio, 1),
    (@emp1, 'Sandra Mara Teixeira',  '41566677788', 'sandra.teixeira@email.com', '16993330005', 'Rua Goias, 77',            @inicio, 1),
    (@emp1, 'Consumidor Balcao A',   NULL,          NULL,                        NULL,          NULL,                       @inicio, 1),
    (@emp1, 'Consumidor Balcao B',   NULL,          NULL,                        NULL,          NULL,                       @inicio, 1),
    (@emp2, 'Eduardo Martins Silva', '42122233344', 'eduardo.silva@email.com',   '16994440001', 'Rua Parana, 230',          @inicio, 1),
    (@emp2, 'Camila Duarte Rocha',   '42233344455', 'camila.rocha@email.com',    '16994440002', 'Av. Santos Dumont, 1450',  @inicio, 1),
    (@emp2, 'Infotech Servicos ME',  '42344455566', 'compras@infotech.com.br',   '16994440003', 'Rua Minas Gerais, 900',    @inicio, 1),
    (@emp2, 'Lucas Andrade Moreira', '42455566677', 'lucas.moreira@email.com',   '16994440004', 'Rua Ceara, 66',            @inicio, 1),
    (@emp2, 'Consumidor Balcao',     NULL,          NULL,                        NULL,          NULL,                       @inicio, 1);

INSERT INTO Tb_Produto
    (empresa_id, categoria_produto_id, nome, descricao, preco_custo, preco_venda,
     quantidade_atual, estoque_minimo, data_cadastro, ativo)
SELECT @emp1, c.id, p.nome, p.descricao, p.custo, p.venda, 0, p.minimo, @inicio, p.ativo
FROM (VALUES
    ('Arroz Tipo 1 5kg',        'Mercearia',  'Arroz agulhinha polido',        18.90,  29.90, 30, 1),
    ('Feijao Carioca 1kg',      'Mercearia',  'Feijao carioca tipo 1',          5.40,   8.99, 40, 1),
    ('Oleo de Soja 900ml',      'Mercearia',  'Oleo de soja refinado',          4.80,   7.49, 50, 1),
    ('Cafe Torrado 500g',       'Mercearia',  'Cafe torrado e moido',          11.20,  18.90, 25, 1),
    ('Macarrao Espaguete 500g', 'Mercearia',  'Massa com ovos',                 3.10,   5.79, 45, 1),
    ('Acucar Refinado 1kg',     'Mercearia',  'Acucar refinado especial',       3.60,   5.99, 40, 1),
    ('Refrigerante Cola 2L',    'Bebidas',    'Refrigerante sabor cola',        5.10,   9.49, 60, 1),
    ('Agua Mineral 1,5L',       'Bebidas',    'Agua mineral sem gas',           1.40,   2.99, 80, 1),
    ('Suco de Uva Integral 1L', 'Bebidas',    'Suco integral sem acucar',       8.60,  14.90, 20, 1),
    ('Cerveja Lata 350ml',      'Bebidas',    'Cerveja pilsen',                 2.70,   4.99, 70, 1),
    ('Detergente Neutro 500ml', 'Limpeza',    'Detergente liquido',             1.90,   3.49, 60, 1),
    ('Sabao em Po 1kg',         'Limpeza',    'Sabao em po multiacao',          9.30,  15.90, 30, 1),
    ('Agua Sanitaria 1L',       'Limpeza',    'Alvejante e desinfetante',       2.80,   4.99, 45, 1),
    ('Papel Higienico 12un',    'Higiene',    'Folha dupla',                   14.50,  24.90, 25, 1),
    ('Sabonete 90g',            'Higiene',    'Sabonete em barra',              1.20,   2.49, 70, 1),
    ('Creme Dental 90g',        'Higiene',    'Creme dental com fluor',         2.40,   4.79, 50, 1),
    ('Banana Prata kg',         'Hortifruti', 'Banana prata selecionada',       3.20,   5.99, 15, 1),
    ('Tomate Italiano kg',      'Hortifruti', 'Tomate italiano',                4.10,   7.49, 15, 1),
    ('Vassoura Descontinuada',  'Limpeza',    'Item fora de linha',             7.00,  12.00, 10, 0)
) AS p(nome, categoria, descricao, custo, venda, minimo, ativo)
JOIN Tb_Categoria_Produto c ON c.empresa_id = @emp1 AND c.nome = p.categoria;

INSERT INTO Tb_Produto
    (empresa_id, categoria_produto_id, nome, descricao, preco_custo, preco_venda,
     quantidade_atual, estoque_minimo, data_cadastro, ativo)
SELECT @emp2, c.id, p.nome, p.descricao, p.custo, p.venda, 0, p.minimo, @inicio, p.ativo
FROM (VALUES
    ('Teclado Mecanico RGB',    'Perifericos', 'Switch blue, ABNT2',        180.00, 319.90, 8,  1),
    ('Mouse Gamer 7200dpi',     'Perifericos', 'Sensor optico, 6 botoes',    75.00, 139.90, 12, 1),
    ('Headset Estereo USB',     'Perifericos', 'Com microfone retratil',    110.00, 199.90, 10, 1),
    ('Webcam Full HD',          'Perifericos', '1080p com microfone',       125.00, 219.90, 6,  1),
    ('Mousepad Speed Grande',   'Perifericos', '900x400mm, base emborrachada', 22.00, 49.90, 15, 1),
    ('SSD 480GB SATA',          'Componentes', 'Leitura ate 550MB/s',       165.00, 279.90, 10, 1),
    ('Memoria DDR4 8GB',        'Componentes', '2666MHz, CL19',             130.00, 219.90, 12, 1),
    ('Fonte 500W Bivolt',       'Componentes', 'Certificacao 80 Plus',      190.00, 329.90, 5,  1),
    ('Cooler para CPU',         'Componentes', 'Air cooler 120mm',           85.00, 149.90, 8,  1),
    ('Cabo HDMI 2m',            'Cabos',       'Versao 2.0, 4K',             14.00,  29.90, 30, 1),
    ('Cabo USB-C 1m',           'Cabos',       'Carga rapida 3A',            11.00,  24.90, 35, 1),
    ('Adaptador USB para RJ45', 'Cabos',       'Gigabit ethernet',           38.00,  74.90, 12, 1),
    ('Roteador Wi-Fi AC1200',   'Redes',       'Dual band, 4 antenas',      145.00, 259.90, 8,  1),
    ('Switch 8 Portas Gigabit', 'Redes',       'Nao gerenciavel',            98.00, 179.90, 6,  1),
    ('Hub USB 2.0 Antigo',      'Perifericos', 'Item fora de linha',         18.00,  39.90, 5,  0)
) AS p(nome, categoria, descricao, custo, venda, minimo, ativo)
JOIN Tb_Categoria_Produto c ON c.empresa_id = @emp2 AND c.nome = p.categoria;

IF OBJECT_ID('tempdb..#Cfg') IS NOT NULL DROP TABLE #Cfg;
CREATE TABLE #Cfg (
    ord             INT PRIMARY KEY,
    empresa_id      INT NOT NULL,
    usu_admin       INT NOT NULL,
    usu_operador    INT NOT NULL,
    usu_estoque     INT NOT NULL,
    tipo_saida      INT NOT NULL,
    tipo_entrada    INT NOT NULL,
    tipo_devolucao  INT NOT NULL,
    tipo_ajuste_sai INT NOT NULL,
    base_vendas     INT NOT NULL,
    base_despesa    DECIMAL(10,2) NOT NULL
);

INSERT INTO #Cfg
SELECT 1, @emp1,
       (SELECT id FROM Tb_Usuario WHERE username = 'admin.bp'),
       (SELECT id FROM Tb_Usuario WHERE username = 'vendedor.bp'),
       (SELECT id FROM Tb_Usuario WHERE username = 'estoquista.bp'),
       (SELECT id FROM Tb_Tipo_Movimentacao WHERE empresa_id = @emp1 AND nome = 'Baixa por venda'),
       (SELECT id FROM Tb_Tipo_Movimentacao WHERE empresa_id = @emp1 AND nome = 'Compra de fornecedor'),
       (SELECT id FROM Tb_Tipo_Movimentacao WHERE empresa_id = @emp1 AND nome = 'Devolucao'),
       (SELECT id FROM Tb_Tipo_Movimentacao WHERE empresa_id = @emp1 AND nome = 'Ajuste de saida'),
       14, 1.00
UNION ALL
SELECT 2, @emp2,
       (SELECT id FROM Tb_Usuario WHERE username = 'admin.ts'),
       (SELECT id FROM Tb_Usuario WHERE username = 'vendedor.ts'),
       (SELECT id FROM Tb_Usuario WHERE username = 'estoquista.ts'),
       (SELECT id FROM Tb_Tipo_Movimentacao WHERE empresa_id = @emp2 AND nome = 'Baixa por venda'),
       (SELECT id FROM Tb_Tipo_Movimentacao WHERE empresa_id = @emp2 AND nome = 'Compra de fornecedor'),
       (SELECT id FROM Tb_Tipo_Movimentacao WHERE empresa_id = @emp2 AND nome = 'Devolucao'),
       (SELECT id FROM Tb_Tipo_Movimentacao WHERE empresa_id = @emp2 AND nome = 'Ajuste de saida'),
       8, 1.00;

IF OBJECT_ID('tempdb..#Prod') IS NOT NULL DROP TABLE #Prod;
CREATE TABLE #Prod (
    empresa_id  INT NOT NULL,
    ord         INT NOT NULL,
    produto_id  INT NOT NULL,
    custo_base  DECIMAL(10,2) NOT NULL,
    venda_base  DECIMAL(10,2) NOT NULL,
    PRIMARY KEY (empresa_id, ord)
);

INSERT INTO #Prod (empresa_id, ord, produto_id, custo_base, venda_base)
SELECT p.empresa_id,
       ROW_NUMBER() OVER (PARTITION BY p.empresa_id ORDER BY p.id),
       p.id, p.preco_custo, p.preco_venda
FROM Tb_Produto p
WHERE p.empresa_id IN (@emp1, @emp2) AND p.ativo = 1;

IF OBJECT_ID('tempdb..#Func') IS NOT NULL DROP TABLE #Func;
CREATE TABLE #Func (
    empresa_id      INT NOT NULL,
    ord             INT NOT NULL,
    funcionario_id  INT NOT NULL,
    PRIMARY KEY (empresa_id, ord)
);

INSERT INTO #Func (empresa_id, ord, funcionario_id)
SELECT f.empresa_id, ROW_NUMBER() OVER (PARTITION BY f.empresa_id ORDER BY f.id), f.id
FROM Tb_Funcionario f
WHERE f.empresa_id IN (@emp1, @emp2) AND f.ativo = 1;

IF OBJECT_ID('tempdb..#Cli') IS NOT NULL DROP TABLE #Cli;
CREATE TABLE #Cli (
    empresa_id  INT NOT NULL,
    ord         INT NOT NULL,
    cliente_id  INT NOT NULL,
    PRIMARY KEY (empresa_id, ord)
);

INSERT INTO #Cli (empresa_id, ord, cliente_id)
SELECT c.empresa_id, ROW_NUMBER() OVER (PARTITION BY c.empresa_id ORDER BY c.id), c.id
FROM Tb_Cliente c
WHERE c.empresa_id IN (@emp1, @emp2) AND c.ativo = 1;

IF OBJECT_ID('tempdb..#FP') IS NOT NULL DROP TABLE #FP;
CREATE TABLE #FP (
    empresa_id          INT NOT NULL,
    ord                 INT NOT NULL,
    forma_pagamento_id  INT NOT NULL,
    PRIMARY KEY (empresa_id, ord)
);

INSERT INTO #FP (empresa_id, ord, forma_pagamento_id)
SELECT f.empresa_id, ROW_NUMBER() OVER (PARTITION BY f.empresa_id ORDER BY f.id), f.id
FROM Tb_Forma_Pagamento f
WHERE f.empresa_id IN (@emp1, @emp2) AND f.ativo = 1;

IF OBJECT_ID('tempdb..#DespModelo') IS NOT NULL DROP TABLE #DespModelo;
CREATE TABLE #DespModelo (
    empresa_id  INT NOT NULL,
    categoria   VARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
    descricao   VARCHAR(255) COLLATE DATABASE_DEFAULT NOT NULL,
    valor_base  DECIMAL(10,2) NOT NULL,
    dia         INT NOT NULL,
    fixa        BIT NOT NULL
);

INSERT INTO #DespModelo (empresa_id, categoria, descricao, valor_base, dia, fixa)
VALUES
    (@emp1, 'Aluguel',            'Aluguel da loja',                     570.00,  5, 1),
    (@emp1, 'Folha de Pagamento', 'Salarios e encargos da equipe',       860.00,  5, 1),
    (@emp1, 'Utilidades',         'Energia, agua e internet',            210.00, 10, 1),
    (@emp1, 'Impostos e Taxas',   'Simples Nacional',                    210.00, 20, 1),
    (@emp2, 'Aluguel',            'Aluguel da loja',                    3850.00,  5, 1),
    (@emp2, 'Folha de Pagamento', 'Salarios e encargos da equipe',     21710.00,  5, 1),
    (@emp2, 'Utilidades',         'Energia eletrica e internet',        1280.00, 10, 1),
    (@emp2, 'Impostos e Taxas',   'Simples Nacional',                   6200.00, 20, 1);

DECLARE @i INT, @cresc INT, @mesRef DATE, @proxMes DATE;
DECLARE @fatorPreco DECIMAL(10,4), @fatorDespesa DECIMAL(10,4);
DECLARE @ordEmp INT, @empV INT;
DECLARE @usuAdmin INT, @usuOperador INT, @usuEstoque INT;
DECLARE @tipoSaida INT, @tipoEntrada INT, @tipoDevolucao INT, @tipoAjusteSai INT;
DECLARE @baseVendas INT, @baseDespesa DECIMAL(10,2);
DECLARE @sazonal INT, @vendasNoMes INT, @v INT, @k INT, @itensNaVenda INT;
DECLARE @totalFunc INT, @totalCli INT, @totalFP INT, @totalProd INT;
DECLARE @dataVenda DATETIME, @dataReposicao DATETIME;
DECLARE @funcId INT, @cliId INT, @fpId INT, @vendaId INT, @prodId INT;
DECLARE @precoV DECIMAL(10,2), @precoC DECIMAL(10,2);
DECLARE @qtd INT, @qtdAntes INT, @qtdDepois INT;
DECLARE @valorTotal DECIMAL(10,2), @desconto DECIMAL(10,2);

SET @i = @meses;

WHILE @i >= 1
BEGIN
    SET @cresc        = @meses + 1 - @i;
    SET @mesRef       = DATEADD(MONTH, -@i, @mesBase);
    SET @proxMes      = DATEADD(MONTH, 1, @mesRef);
    SET @fatorPreco   = 1.0 + (0.008 * @cresc);
    SET @fatorDespesa = 1.0 + (0.0025 * @cresc);

    SET @ordEmp = 1;

    WHILE @ordEmp <= 2
    BEGIN
        SELECT @empV          = empresa_id,
               @usuAdmin      = usu_admin,
               @usuOperador   = usu_operador,
               @usuEstoque    = usu_estoque,
               @tipoSaida     = tipo_saida,
               @tipoEntrada   = tipo_entrada,
               @tipoDevolucao = tipo_devolucao,
               @tipoAjusteSai = tipo_ajuste_sai,
               @baseVendas    = base_vendas,
               @baseDespesa   = base_despesa
          FROM #Cfg WHERE ord = @ordEmp;

        SET @dataReposicao = DATEADD(HOUR, 8, CAST(@mesRef AS DATETIME));

        INSERT INTO Tb_Movimentacao_Estoque
            (empresa_id, produto_id, usuario_id, venda_id, tipo_movimentacao_id,
             quantidade, quantidade_antes, quantidade_depois, data_movimentacao, observacao)
        SELECT @empV, p.id, @usuEstoque, NULL, @tipoEntrada,
               meta.alvo - p.quantidade_atual, p.quantidade_atual, meta.alvo,
               @dataReposicao, 'Reposicao mensal de estoque'
        FROM Tb_Produto p
        CROSS APPLY (SELECT CASE WHEN p.estoque_minimo * 10 < 140
                                 THEN 140 ELSE p.estoque_minimo * 10 END AS alvo) AS meta
        WHERE p.empresa_id = @empV
          AND p.ativo = 1
          AND p.estoque_minimo IS NOT NULL
          AND p.quantidade_atual < meta.alvo;

        UPDATE p
           SET p.quantidade_atual = CASE WHEN p.estoque_minimo * 10 < 140
                                         THEN 140 ELSE p.estoque_minimo * 10 END
          FROM Tb_Produto p
         WHERE p.empresa_id = @empV
           AND p.ativo = 1
           AND p.estoque_minimo IS NOT NULL
           AND p.quantidade_atual < CASE WHEN p.estoque_minimo * 10 < 140
                                         THEN 140 ELSE p.estoque_minimo * 10 END;


        SELECT @totalFunc = COUNT(*) FROM #Func WHERE empresa_id = @empV;
        SELECT @totalCli  = COUNT(*) FROM #Cli  WHERE empresa_id = @empV;
        SELECT @totalFP   = COUNT(*) FROM #FP   WHERE empresa_id = @empV;
        SELECT @totalProd = COUNT(*) FROM #Prod WHERE empresa_id = @empV;

        SET @vendasNoMes = 10 * @totalFunc;

        SET @v = 1;

        WHILE @v <= @vendasNoMes
        BEGIN
            SET @dataVenda = DATEADD(HOUR, 9 + (@v % 10),
                             CAST(DATEFROMPARTS(YEAR(@mesRef), MONTH(@mesRef),
                                                1 + ((@v * 3) % 27)) AS DATETIME));

            SELECT @funcId = funcionario_id FROM #Func
             WHERE empresa_id = @empV AND ord = (@v % @totalFunc) + 1;

            SELECT @cliId = cliente_id FROM #Cli
             WHERE empresa_id = @empV AND ord = ((@v + @i) % @totalCli) + 1;

            SELECT @fpId = forma_pagamento_id FROM #FP
             WHERE empresa_id = @empV AND ord = ((@v + @i * 2) % @totalFP) + 1;

            IF (@v % 6 = 0) SET @cliId = NULL;

            INSERT INTO Tb_Venda (empresa_id, funcionario_id, cliente_id, forma_pagamento_id,
                                  data_venda, valor_total, desconto, valor_final,
                                  observacao, situacao_venda)
            VALUES (@empV, @funcId, @cliId, @fpId, @dataVenda, 0, 0, 0, NULL, 'CONCLUIDA');

            SET @vendaId    = SCOPE_IDENTITY();
            SET @valorTotal = 0;
            SET @itensNaVenda = 2 + ((@v + @i) % 3);
            SET @k = 0;

            WHILE @k < @itensNaVenda
            BEGIN
                SELECT @prodId = produto_id,
                       @precoV = ROUND(venda_base * @fatorPreco, 2),
                       @precoC = ROUND(custo_base * @fatorPreco, 2)
                  FROM #Prod
                 WHERE empresa_id = @empV
                   AND ord = (((@v * 5) + (@k * 3) + @i) % @totalProd) + 1;

                IF NOT EXISTS (SELECT 1 FROM Tb_Item_Venda
                                WHERE venda_id = @vendaId AND produto_id = @prodId)
                BEGIN
                    SET @qtd = 1 + ((@prodId + @v / 2 + @k * 5) % 4);

                    SELECT @qtdAntes = quantidade_atual FROM Tb_Produto WHERE id = @prodId;
                    SET @qtdDepois = @qtdAntes - @qtd;

                    IF @qtdDepois >= 0
                    BEGIN
                        INSERT INTO Tb_Item_Venda
                            (venda_id, produto_id, quantidade, preco_unitario, preco_custo)
                        VALUES (@vendaId, @prodId, @qtd, @precoV, @precoC);

                        INSERT INTO Tb_Movimentacao_Estoque
                            (empresa_id, produto_id, usuario_id, venda_id, tipo_movimentacao_id,
                             quantidade, quantidade_antes, quantidade_depois,
                             data_movimentacao, observacao)
                        VALUES (@empV, @prodId, @usuOperador, @vendaId, @tipoSaida,
                                @qtd, @qtdAntes, @qtdDepois, @dataVenda,
                                'Baixa automatica pela venda.');

                        UPDATE Tb_Produto SET quantidade_atual = @qtdDepois WHERE id = @prodId;

                        SET @valorTotal = @valorTotal + (@precoV * @qtd);
                    END;
                END;

                SET @k = @k + 1;
            END;

            IF @valorTotal > 0
            BEGIN
                SET @desconto = CASE WHEN @v % 5 = 0 THEN ROUND(@valorTotal * 0.05, 2) ELSE 0 END;

                UPDATE Tb_Venda
                   SET valor_total = @valorTotal,
                       desconto    = @desconto,
                       valor_final = @valorTotal - @desconto
                 WHERE id = @vendaId;

                IF @i % 5 = 0 AND @v = 3
                BEGIN
                    INSERT INTO Tb_Movimentacao_Estoque
                        (empresa_id, produto_id, usuario_id, venda_id, tipo_movimentacao_id,
                         quantidade, quantidade_antes, quantidade_depois,
                         data_movimentacao, observacao)
                    SELECT @empV, iv.produto_id, @usuOperador, @vendaId, @tipoDevolucao,
                           iv.quantidade, p.quantidade_atual, p.quantidade_atual + iv.quantidade,
                           DATEADD(DAY, 1, @dataVenda),
                           'Estorno de estoque pelo cancelamento da venda.'
                      FROM Tb_Item_Venda iv
                      JOIN Tb_Produto p ON p.id = iv.produto_id
                     WHERE iv.venda_id = @vendaId;

                    UPDATE p
                       SET p.quantidade_atual = p.quantidade_atual + iv.quantidade
                      FROM Tb_Produto p
                      JOIN Tb_Item_Venda iv ON iv.produto_id = p.id
                     WHERE iv.venda_id = @vendaId;

                    UPDATE Tb_Venda
                       SET situacao_venda = 'CANCELADA',
                           observacao     = 'Cancelada a pedido do cliente.'
                     WHERE id = @vendaId;

                    INSERT INTO Tb_Log_Sistema
                        (empresa_id, usuario_id, acao, entidade_afetada, registro_id, data_hora, detalhes)
                    VALUES (@empV, @usuAdmin, 'CANCELAMENTO', 'Venda', @vendaId,
                            DATEADD(DAY, 1, @dataVenda),
                            'Venda cancelada e estoque estornado.');
                END;
            END
            ELSE
            BEGIN
                DELETE FROM Tb_Venda WHERE id = @vendaId;
            END;

            SET @v = @v + 1;
        END;

        INSERT INTO Tb_Log_Sistema
            (empresa_id, usuario_id, acao, entidade_afetada, registro_id, data_hora, detalhes)
        SELECT v.empresa_id, @usuOperador, 'CRIACAO', 'Venda', v.id, v.data_venda,
               'Venda registrada com valor final ' + CONVERT(VARCHAR(20), v.valor_final)
          FROM Tb_Venda v
         WHERE v.empresa_id = @empV
           AND v.data_venda >= @mesRef
           AND v.data_venda <  @proxMes;

        INSERT INTO Tb_Despesa
            (empresa_id, categoria_despesa_id, usuario_id, descricao, valor, data_despesa, fixa, observacao)
        SELECT @empV, cd.id, @usuAdmin, d.descricao,
               ROUND(d.valor_base * @fatorDespesa, 2),
               DATEFROMPARTS(YEAR(@mesRef), MONTH(@mesRef), d.dia),
               d.fixa, NULL
          FROM #DespModelo d
          JOIN Tb_Categoria_Despesa cd
            ON cd.empresa_id = @empV AND cd.nome = d.categoria COLLATE DATABASE_DEFAULT
         WHERE d.empresa_id = @empV;

        SET @ordEmp = @ordEmp + 1;
    END;

    SET @i = @i - 1;
END;

UPDATE p
   SET p.preco_custo = ROUND(b.custo_base * (1.0 + 0.008 * @meses), 2),
       p.preco_venda = ROUND(b.venda_base * (1.0 + 0.008 * @meses), 2)
  FROM Tb_Produto p
  JOIN #Prod b ON b.produto_id = p.id;

INSERT INTO Tb_Log_Sistema
    (empresa_id, usuario_id, acao, entidade_afetada, registro_id, data_hora, detalhes)
SELECT c.empresa_id, c.usu_admin, 'ALTERACAO', 'Produto', p.id,
       DATEADD(DAY, -3, CAST(@mesBase AS DATETIME)),
       'Reajuste de tabela de precos aplicado.'
  FROM Tb_Produto p
  JOIN #Cfg c ON c.empresa_id = p.empresa_id
 WHERE p.ativo = 1;

IF OBJECT_ID('tempdb..#Baixo') IS NOT NULL DROP TABLE #Baixo;
CREATE TABLE #Baixo (produto_id INT PRIMARY KEY, empresa_id INT, alvo INT, atual INT);

INSERT INTO #Baixo (produto_id, empresa_id, alvo, atual)
SELECT x.id, x.empresa_id,
       CASE WHEN x.estoque_minimo > 2 THEN x.estoque_minimo - 2 ELSE 0 END,
       x.quantidade_atual
FROM (
    SELECT p.id, p.empresa_id, p.estoque_minimo, p.quantidade_atual,
           ROW_NUMBER() OVER (PARTITION BY p.empresa_id ORDER BY p.id DESC) AS rn
      FROM Tb_Produto p
     WHERE p.ativo = 1 AND p.estoque_minimo IS NOT NULL
) AS x
WHERE x.rn <= 2 AND x.quantidade_atual > x.estoque_minimo;

INSERT INTO Tb_Movimentacao_Estoque
    (empresa_id, produto_id, usuario_id, venda_id, tipo_movimentacao_id,
     quantidade, quantidade_antes, quantidade_depois, data_movimentacao, observacao)
SELECT b.empresa_id, b.produto_id, c.usu_estoque, NULL, c.tipo_ajuste_sai,
       b.atual - b.alvo, b.atual, b.alvo,
       DATEADD(DAY, -2, CAST(@mesBase AS DATETIME)),
       'Ajuste de inventario apos conferencia fisica.'
  FROM #Baixo b
  JOIN #Cfg c ON c.empresa_id = b.empresa_id
 WHERE b.atual > b.alvo;

UPDATE p
   SET p.quantidade_atual = b.alvo
  FROM Tb_Produto p
  JOIN #Baixo b ON b.produto_id = p.id;

INSERT INTO Tb_Log_Sistema
    (empresa_id, usuario_id, acao, entidade_afetada, registro_id, data_hora, detalhes)
SELECT b.empresa_id, c.usu_estoque, 'ALTERACAO', 'MovimentacaoEstoque', b.produto_id,
       DATEADD(DAY, -2, CAST(@mesBase AS DATETIME)),
       'Ajuste de saida lancado apos conferencia de inventario.'
  FROM #Baixo b
  JOIN #Cfg c ON c.empresa_id = b.empresa_id;

DROP TABLE #Cfg;
DROP TABLE #Prod;
DROP TABLE #Func;
DROP TABLE #Cli;
DROP TABLE #FP;
DROP TABLE #DespModelo;
DROP TABLE #Baixo;

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT e.nome                                                                   AS empresa,
       (SELECT COUNT(*) FROM Tb_Usuario              WHERE empresa_id = e.id)   AS usuarios,
       (SELECT COUNT(*) FROM Tb_Funcionario          WHERE empresa_id = e.id)   AS funcionarios,
       (SELECT COUNT(*) FROM Tb_Cliente              WHERE empresa_id = e.id)   AS clientes,
       (SELECT COUNT(*) FROM Tb_Produto              WHERE empresa_id = e.id)   AS produtos,
       (SELECT COUNT(*) FROM Tb_Venda                WHERE empresa_id = e.id)   AS vendas,
       (SELECT COUNT(*) FROM Tb_Venda                WHERE empresa_id = e.id
                                                       AND situacao_venda = 'CANCELADA') AS canceladas,
       (SELECT COUNT(*) FROM Tb_Movimentacao_Estoque WHERE empresa_id = e.id)   AS movimentacoes,
       (SELECT COUNT(*) FROM Tb_Despesa              WHERE empresa_id = e.id)   AS despesas,
       (SELECT COUNT(*) FROM Tb_Log_Sistema          WHERE empresa_id = e.id)   AS logs
  FROM Tb_Empresa e
 WHERE e.cnpj IN ('11222333000181', '44555666000199');

SELECT e.nome                                       AS empresa,
       YEAR(v.data_venda)                           AS ano,
       MONTH(v.data_venda)                          AS mes,
       COUNT(*)                                     AS vendas,
       CAST(SUM(v.valor_final) AS DECIMAL(12,2))    AS faturamento
  FROM Tb_Venda v
  JOIN Tb_Empresa e ON e.id = v.empresa_id
 WHERE e.cnpj IN ('11222333000181', '44555666000199')
   AND v.situacao_venda = 'CONCLUIDA'
 GROUP BY e.nome, YEAR(v.data_venda), MONTH(v.data_venda)
 ORDER BY e.nome, ano, mes;

SELECT e.nome AS empresa,
       CAST(r.receita AS DECIMAL(14,2))                                   AS receita_bruta,
       CAST(c.custo AS DECIMAL(14,2))                                     AS custo_produtos,
       CAST(d.despesas AS DECIMAL(14,2))                                  AS despesas,
       CAST(r.receita - c.custo - d.despesas AS DECIMAL(14,2))            AS resultado,
       CAST((r.receita - c.custo - d.despesas) * 100 / r.receita AS DECIMAL(6,2)) AS margem_pct
  FROM Tb_Empresa e
 CROSS APPLY (SELECT SUM(v.valor_final) AS receita
                FROM Tb_Venda v
               WHERE v.empresa_id = e.id AND v.situacao_venda = 'CONCLUIDA') r
 CROSS APPLY (SELECT SUM(i.preco_custo * i.quantidade) AS custo
                FROM Tb_Item_Venda i
                JOIN Tb_Venda v ON v.id = i.venda_id
               WHERE v.empresa_id = e.id AND v.situacao_venda = 'CONCLUIDA') c
 CROSS APPLY (SELECT SUM(x.valor) AS despesas
                FROM Tb_Despesa x
               WHERE x.empresa_id = e.id) d
 WHERE e.cnpj IN ('11222333000181', '44555666000199')
 ORDER BY e.nome;
GO