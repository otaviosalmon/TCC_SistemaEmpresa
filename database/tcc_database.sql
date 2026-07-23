-- ================================================================
--  SISTEMA DE GESTÃO COMERCIAL
--  Script de Criação do Banco de Dados — SQL Server 2019+
--
--  CONVENÇÕES ADOTADAS:
--    • PK_<Tabela>                  → Primary Key
--    • FK_<Tabela>_<Referenciada>   → Foreign Key
--    • UQ_<Tabela>_<Coluna>         → Unique Constraint
--    • IX_<Tabela>_<Coluna>         → Index não-clusterizado
--    • CHK_<Tabela>_<Regra>         → Check Constraint
--
--  POR QUE ISSO IMPORTA:
--    Nomes de constraints explícitos permitem que erros de banco
--    sejam rastreáveis (a mensagem de erro cita o nome da constraint).
--    Sem nomes, o SQL Server gera algo como "FK__Tb_Venda__cliente__3D5E1FD2"
--    — inútil em produção.
--
--  ORDEM DE CRIAÇÃO:
--    Tabelas são criadas na ordem topológica das dependências.
--    Criar uma FK para uma tabela que ainda não existe gera erro.
-- ================================================================

USE master;
GO

-- ================================================================
-- CRIAÇÃO DO BANCO DE DADOS
-- ================================================================
-- Por que configurar RECOVERY MODEL?
--   FULL  → Permite backup incremental/log. Obrigatório em produção.
--   SIMPLE → SQL Server descarta o log após cada checkpoint. Só para dev.
-- Por que dois arquivos (.mdf e .ldf)?
--   MDF = dados; LDF = log de transações. Separar em discos físicos
--   diferentes melhora drasticamente a performance em I/O intenso.

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'SistemaGestaoComercial')
BEGIN
    CREATE DATABASE SistemaGestaoComercial
    COLLATE Latin1_General_CI_AI;   -- CI = Case Insensitive | AI = Accent Insensitive
                                    -- Padrão para sistemas em português.
                                    -- Permite buscar "JOSE" e encontrar "José".
END;
GO

USE SistemaGestaoComercial;
GO

-- ================================================================
-- SEÇÃO 1 — TABELA RAIZ DO SISTEMA
-- ================================================================
-- Tb_Empresa é a âncora de todo o modelo multi-tenant.
-- MULTI-TENANT: um único banco serve múltiplas empresas.
-- Cada tabela terá empresa_id, garantindo isolamento de dados.
-- Vantagem: uma só instância de aplicação atende N clientes.

CREATE TABLE Tb_Empresa (
    id          INT             NOT NULL IDENTITY(1,1),
    nome        VARCHAR(150)    NOT NULL,
    cnpj        VARCHAR(14)     NOT NULL,
    email       VARCHAR(150)        NULL,
    endereco    VARCHAR(200)        NULL,
    cidade      VARCHAR(100)        NULL,
    -- CHAR(2) em vez de VARCHAR(2): estado sempre terá exatamente 2 chars (SP, RJ…).
    -- CHAR não desperdiça espaço em valor fixo e é marginalmente mais rápido em índices.
    estado      CHAR(2)             NULL,
    cep         VARCHAR(10)         NULL,
    telefone    VARCHAR(20)         NULL,
    -- BIT é o tipo correto para booleano no SQL Server.
    -- Usar TINYINT ou INT para isso é anti-pattern.
    ativo       BIT             NOT NULL CONSTRAINT DF_Empresa_Ativo DEFAULT 1,

    CONSTRAINT PK_Empresa       PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_Empresa_CNPJ  UNIQUE (cnpj),

    -- CHECK: CNPJ deve ter exatamente 14 dígitos numéricos.
    -- A validação de algorítmo do CNPJ fica na camada de aplicação,
    -- mas o banco garante o formato mínimo.
    CONSTRAINT CHK_Empresa_CNPJ CHECK (LEN(cnpj) = 14 AND cnpj NOT LIKE '%[^0-9]%')
);
GO

-- ================================================================
-- SEÇÃO 2 — TABELAS DE DOMÍNIO / LOOKUP
-- (Dependem apenas de Tb_Empresa)
-- ================================================================

-- Por que criar tabelas de lookup antes das transacionais?
-- As tabelas de venda, produto etc. referenciam categorias, formas
-- de pagamento e cargos. Cria-se primeiro o que é referenciado.

CREATE TABLE Tb_Categoria_Produto (
    id          INT             NOT NULL IDENTITY(1,1),
    empresa_id  INT             NOT NULL,
    nome        VARCHAR(150)    NOT NULL,
    descricao   VARCHAR(255)        NULL,

    CONSTRAINT PK_CategoriaProduto              PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_CategoriaProduto_Empresa      FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id)
);
GO

CREATE TABLE Tb_Categoria_Despesa (
    id          INT             NOT NULL IDENTITY(1,1),
    empresa_id  INT             NOT NULL,
    nome        VARCHAR(100)    NOT NULL,
    descricao   VARCHAR(255)        NULL,
    ativo       BIT             NOT NULL CONSTRAINT DF_CategoriaDespesa_Ativo DEFAULT 1,

    CONSTRAINT PK_CategoriaDespesa              PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_CategoriaDespesa_Empresa      FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id)
);
GO

CREATE TABLE Tb_Forma_Pagamento (
    id          INT             NOT NULL IDENTITY(1,1),
    empresa_id  INT             NOT NULL,
    nome        VARCHAR(50)     NOT NULL,
    descricao   VARCHAR(150)        NULL,
    ativo       BIT             NOT NULL CONSTRAINT DF_FormaPagamento_Ativo DEFAULT 1,

    CONSTRAINT PK_FormaPagamento                PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_FormaPagamento_Empresa        FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id)
);
GO

CREATE TABLE Tb_Tipo_Movimentacao (
    id          INT             NOT NULL IDENTITY(1,1),
    empresa_id  INT             NOT NULL,
    nome        VARCHAR(100)    NOT NULL,
    -- natureza: 'ENTRADA' ou 'SAIDA' — controla a direção do estoque.
    -- CHECK garante que apenas valores válidos sejam inseridos.
    -- Alternativa mais robusta seria uma tabela enum, mas para 2 valores
    -- o CHECK é mais simples e performático.
    natureza    VARCHAR(10)     NOT NULL,
    descricao   VARCHAR(255)        NULL,
    ativo       BIT             NOT NULL CONSTRAINT DF_TipoMovimentacao_Ativo DEFAULT 1,

    CONSTRAINT PK_TipoMovimentacao              PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_TipoMovimentacao_Empresa      FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT CHK_TipoMovimentacao_Natureza    CHECK (natureza IN ('ENTRADA', 'SAIDA'))
);
GO

CREATE TABLE Tb_Cargo (
    id                  INT             NOT NULL IDENTITY(1,1),
    empresa_id          INT             NOT NULL,
    nome                VARCHAR(100)    NOT NULL,
    descricao           VARCHAR(255)        NULL,
    salario_base        DECIMAL(10,2)       NULL,
    per_comissao_base   DECIMAL(5,2)        NULL,
    ativo               BIT             NOT NULL CONSTRAINT DF_Cargo_Ativo DEFAULT 1,

    CONSTRAINT PK_Cargo                         PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Cargo_Empresa                 FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    -- Percentual de comissão não pode ser negativo nem ultrapassar 100%.
    CONSTRAINT CHK_Cargo_Comissao               CHECK (per_comissao_base IS NULL
        OR (per_comissao_base >= 0 AND per_comissao_base <= 100))
);
GO

-- ================================================================
-- SEÇÃO 3 — USUÁRIOS E FUNCIONÁRIOS
-- ================================================================
-- Por que Tb_Usuario existe separado de Tb_Funcionario?
--   Separação de responsabilidades (SRP — Single Responsibility Principle):
--   Tb_Usuario gerencia AUTENTICAÇÃO (login, senha, permissão).
--   Tb_Funcionario gerencia o CONTRATO DE TRABALHO (salário, cargo, admissão).
--   Um usuário pode existir sem ser funcionário (ex: admin do sistema).
--   Um funcionário pode ser desativado do sistema sem excluir o histórico.

CREATE TABLE Tb_Usuario (
    id              INT             NOT NULL IDENTITY(1,1),
    empresa_id      INT             NOT NULL,
    username        VARCHAR(50)     NOT NULL,
    email           VARCHAR(150)    NOT NULL,
    -- password_hash: NUNCA armazene senha em texto puro.
    -- 255 chars comporta hashes BCrypt ($2b$12$...) com segurança.
    password_hash   VARCHAR(255)    NOT NULL,
    -- role: VARCHAR(30) corrigido do esquema original (estava como integer(30),
    -- o que não faz sentido semântico). Role é um nome: 'ADMIN', 'VENDEDOR' etc.
    role            VARCHAR(30)     NOT NULL,
    ativo           BIT             NOT NULL CONSTRAINT DF_Usuario_Ativo DEFAULT 1,
    data_cadastro   DATETIME        NOT NULL CONSTRAINT DF_Usuario_DataCadastro DEFAULT GETDATE(),

    CONSTRAINT PK_Usuario                       PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Usuario_Empresa               FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT UQ_Usuario_Username              UNIQUE (empresa_id, username),
    CONSTRAINT UQ_Usuario_Email                 UNIQUE (empresa_id, email),
    -- Roles válidos no sistema.
    CONSTRAINT CHK_Usuario_Role                 CHECK (role IN ('ADMIN', 'GERENTE', 'VENDEDOR', 'CAIXA', 'ESTOQUISTA'))
);
GO

CREATE TABLE Tb_Funcionario (
    id                  INT             NOT NULL IDENTITY(1,1),
    empresa_id          INT             NOT NULL,
    usuario_id          INT                 NULL,   -- NULL: funcionário sem acesso ao sistema
    cargo_id            INT             NOT NULL,
    nome                VARCHAR(150)    NOT NULL,
    cpf                 VARCHAR(11)     NOT NULL,
    telefone            VARCHAR(20)         NULL,
    endereco            VARCHAR(200)        NULL,
    salario             DECIMAL(10,2)       NULL,
    per_comissao        DECIMAL(5,2)        NULL,
    data_admissao       DATE            NOT NULL,   -- DATE (não DATETIME): só a data importa aqui.
    ativo               BIT             NOT NULL CONSTRAINT DF_Funcionario_Ativo DEFAULT 1,

    CONSTRAINT PK_Funcionario                   PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Funcionario_Empresa           FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_Funcionario_Usuario           FOREIGN KEY (usuario_id)
        REFERENCES Tb_Usuario (id),
    CONSTRAINT FK_Funcionario_Cargo             FOREIGN KEY (cargo_id)
        REFERENCES Tb_Cargo (id),
    -- CPF único por empresa (não globalmente, pois o mesmo funcionário
    -- poderia estar em duas empresas do sistema).
    CONSTRAINT UQ_Funcionario_CPF               UNIQUE (empresa_id, cpf),
    CONSTRAINT CHK_Funcionario_CPF              CHECK (LEN(cpf) = 11 AND cpf NOT LIKE '%[^0-9]%'),
    CONSTRAINT CHK_Funcionario_Comissao         CHECK (per_comissao IS NULL
        OR (per_comissao >= 0 AND per_comissao <= 100))
);
GO

-- ================================================================
-- SEÇÃO 4 — CLIENTES E PRODUTOS
-- ================================================================

CREATE TABLE Tb_Cliente (
    id              INT             NOT NULL IDENTITY(1,1),
    empresa_id      INT             NOT NULL,
    nome            VARCHAR(150)    NOT NULL,
    cpf             VARCHAR(11)         NULL,   -- NULL: permite cliente sem CPF (ex: venda balcão)
    email           VARCHAR(150)        NULL,
    telefone        VARCHAR(20)         NULL,
    endereco        VARCHAR(255)        NULL,
    data_cadastro   DATETIME        NOT NULL CONSTRAINT DF_Cliente_DataCadastro DEFAULT GETDATE(),
    ativo           BIT             NOT NULL CONSTRAINT DF_Cliente_Ativo DEFAULT 1,

    CONSTRAINT PK_Cliente                       PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Cliente_Empresa               FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT UQ_Cliente_CPF                   UNIQUE (empresa_id, cpf),
    CONSTRAINT UQ_Cliente_Email                 UNIQUE (empresa_id, email),
    CONSTRAINT CHK_Cliente_CPF                  CHECK (cpf IS NULL
        OR (LEN(cpf) = 11 AND cpf NOT LIKE '%[^0-9]%'))
);
GO

CREATE TABLE Tb_Produto (
    id                      INT             NOT NULL IDENTITY(1,1),
    empresa_id              INT             NOT NULL,
    categoria_produto_id    INT             NOT NULL,
    nome                    VARCHAR(150)    NOT NULL,
    descricao               VARCHAR(255)        NULL,
    -- DECIMAL(10,2): até 99.999.999,99 — adequado para preços em R$.
    preco_custo             DECIMAL(10,2)   NOT NULL,
    preco_venda             DECIMAL(10,2)   NOT NULL,
    quantidade_atual        INT             NOT NULL CONSTRAINT DF_Produto_QtdAtual DEFAULT 0,
    estoque_minimo          INT                 NULL,
    data_cadastro           DATETIME        NOT NULL CONSTRAINT DF_Produto_DataCadastro DEFAULT GETDATE(),
    ativo                   BIT             NOT NULL CONSTRAINT DF_Produto_Ativo DEFAULT 1,

    CONSTRAINT PK_Produto                       PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Produto_Empresa               FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_Produto_CategoriaProduto      FOREIGN KEY (categoria_produto_id)
        REFERENCES Tb_Categoria_Produto (id),
    -- Regra de negócio: preço de venda nunca pode ser menor que o custo.
    -- Isso protege margem de lucro a nível de banco, independente da aplicação.
    CONSTRAINT CHK_Produto_Preco               CHECK (preco_venda >= preco_custo),
    CONSTRAINT CHK_Produto_QtdAtual            CHECK (quantidade_atual >= 0),
    CONSTRAINT CHK_Produto_EstoqueMinimo       CHECK (estoque_minimo IS NULL OR estoque_minimo >= 0)
);
GO

-- ================================================================
-- SEÇÃO 5 — VENDAS E ITENS
-- ================================================================
-- Venda usa um padrão HEADER/DETAIL (cabeçalho/detalhe).
-- Tb_Venda = cabeçalho (quem vendeu, quando, total).
-- Tb_Item_Venda = detalhe (o que foi vendido, quantidade, preço).
-- Essa separação é fundamental: uma venda tem N itens.

CREATE TABLE Tb_Venda (
    id                  INT             NOT NULL IDENTITY(1,1),
    empresa_id          INT             NOT NULL,
    funcionario_id      INT             NOT NULL,
    cliente_id          INT                 NULL,   -- NULL: venda sem cliente identificado
    forma_pagamento_id  INT             NOT NULL,
    data_venda          DATETIME        NOT NULL CONSTRAINT DF_Venda_DataVenda DEFAULT GETDATE(),
    valor_total         DECIMAL(10,2)   NOT NULL,
    desconto            DECIMAL(10,2)   NOT NULL CONSTRAINT DF_Venda_Desconto DEFAULT 0,
    valor_final         DECIMAL(10,2)   NOT NULL,
    observacao          VARCHAR(255)        NULL,

    CONSTRAINT PK_Venda                         PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Venda_Empresa                 FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_Venda_Funcionario             FOREIGN KEY (funcionario_id)
        REFERENCES Tb_Funcionario (id),
    CONSTRAINT FK_Venda_Cliente                 FOREIGN KEY (cliente_id)
        REFERENCES Tb_Cliente (id),
    CONSTRAINT FK_Venda_FormaPagamento          FOREIGN KEY (forma_pagamento_id)
        REFERENCES Tb_Forma_Pagamento (id),
    CONSTRAINT CHK_Venda_Desconto               CHECK (desconto >= 0),
    -- valor_final deve ser igual a valor_total - desconto.
    -- Isso evita inconsistências de cálculo vindas da aplicação.
    CONSTRAINT CHK_Venda_ValorFinal             CHECK (valor_final = valor_total - desconto)
);
GO

CREATE TABLE Tb_Item_Venda (
    id              INT             NOT NULL IDENTITY(1,1),
    venda_id        INT             NOT NULL,
    produto_id      INT             NOT NULL,
    quantidade      INT             NOT NULL,
    preco_unitario  DECIMAL(10,2)   NOT NULL,
    -- subtotal é um dado derivado (quantidade * preco_unitario).
    -- Armazenamos fisicamente por performance (evita recalcular em relatórios)
    -- e por auditoria (o preço pode mudar; o subtotal da época fica registrado).
    subtotal        AS (quantidade * preco_unitario) PERSISTED,
    -- PERSISTED: o SQL Server calcula uma vez e armazena no disco.
    -- Sem PERSISTED, seria calculado a cada leitura (mais lento em tabelas grandes).

    CONSTRAINT PK_ItemVenda                     PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_ItemVenda_Venda               FOREIGN KEY (venda_id)
        REFERENCES Tb_Venda (id),
    CONSTRAINT FK_ItemVenda_Produto             FOREIGN KEY (produto_id)
        REFERENCES Tb_Produto (id),
    CONSTRAINT CHK_ItemVenda_Quantidade         CHECK (quantidade > 0),
    CONSTRAINT CHK_ItemVenda_Preco              CHECK (preco_unitario > 0)
);
GO

-- ================================================================
-- SEÇÃO 6 — DESPESAS
-- ================================================================

CREATE TABLE Tb_Despesa (
    id                      INT             NOT NULL IDENTITY(1,1),
    empresa_id              INT             NOT NULL,
    categoria_despesa_id    INT             NOT NULL,
    usuario_id              INT             NOT NULL,
    descricao               VARCHAR(255)        NULL,
    valor                   DECIMAL(10,2)   NOT NULL,
    data_despesa            DATE            NOT NULL,   -- DATE: data do fato, não timestamp.
    fixa                    BIT             NOT NULL CONSTRAINT DF_Despesa_Fixa DEFAULT 0,
    observacao              VARCHAR(255)        NULL,

    CONSTRAINT PK_Despesa                       PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Despesa_Empresa               FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_Despesa_CategoriaDespesa      FOREIGN KEY (categoria_despesa_id)
        REFERENCES Tb_Categoria_Despesa (id),
    CONSTRAINT FK_Despesa_Usuario               FOREIGN KEY (usuario_id)
        REFERENCES Tb_Usuario (id),
    CONSTRAINT CHK_Despesa_Valor                CHECK (valor > 0)
);
GO

-- ================================================================
-- SEÇÃO 7 — LOG E AUDITORIA
-- ================================================================
-- Tb_Log_Sistema é uma tabela de auditoria (audit trail).
-- Registra QUEM fez O QUÊ e QUANDO, em QUAL registro.
-- IMPORTANTE: Esta tabela nunca deve ter dados deletados.
-- Por isso, NÃO há FK com ON DELETE CASCADE aqui.
-- Se um usuário for excluído, o log precisa permanecer.

CREATE TABLE Tb_Log_Sistema (
    id                  BIGINT          NOT NULL IDENTITY(1,1),  -- BIGINT: logs crescem muito. INT pode estourar.
    empresa_id          INT             NOT NULL,
    usuario_id          INT                 NULL,   -- NULL: ações do sistema sem usuário logado
    acao                VARCHAR(50)     NOT NULL,
    entidade_afetada    VARCHAR(100)    NOT NULL,
    registro_id         INT                 NULL,
    data_hora           DATETIME        NOT NULL CONSTRAINT DF_LogSistema_DataHora DEFAULT GETDATE(),
    -- detalhes: corrigido de "integer(255)" (tipo inválido) para VARCHAR(MAX).
    -- Armazena JSON, XML ou texto livre com contexto da ação.
    detalhes            VARCHAR(MAX)        NULL,

    CONSTRAINT PK_LogSistema                    PRIMARY KEY CLUSTERED (id),
    -- FKs sem ON DELETE CASCADE: preservar log mesmo se empresa/usuário forem excluídos.
    CONSTRAINT FK_LogSistema_Empresa            FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_LogSistema_Usuario            FOREIGN KEY (usuario_id)
        REFERENCES Tb_Usuario (id)
);
GO

-- ================================================================
-- SEÇÃO 8 — MOVIMENTAÇÃO DE ESTOQUE
-- ================================================================
-- Esta tabela é o livro-razão do estoque.
-- Cada linha é um evento de entrada ou saída.
-- O estoque atual em Tb_Produto é uma projeção deste histórico.
-- Isso permite reconstruir o estoque em qualquer ponto no tempo.

CREATE TABLE Tb_Movimentacao_Estoque (
    id                      INT             NOT NULL IDENTITY(1,1),
    empresa_id              INT             NOT NULL,
    produto_id              INT             NOT NULL,
    usuario_id              INT             NOT NULL,
    venda_id                INT                 NULL,  -- NULL: movimentação manual (ajuste de estoque)
    tipo_movimentacao_id    INT             NOT NULL,
    quantidade              INT             NOT NULL,
    quantidade_antes        INT                 NULL,
    quantidade_depois       INT                 NULL,
    data_movimentacao       DATETIME        NOT NULL CONSTRAINT DF_MovEstoque_DataMovimentacao DEFAULT GETDATE(),
    observacao              VARCHAR(255)        NULL,

    CONSTRAINT PK_MovimentacaoEstoque               PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_MovEstoque_Empresa                FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_MovEstoque_Produto                FOREIGN KEY (produto_id)
        REFERENCES Tb_Produto (id),
    CONSTRAINT FK_MovEstoque_Usuario                FOREIGN KEY (usuario_id)
        REFERENCES Tb_Usuario (id),
    CONSTRAINT FK_MovEstoque_Venda                  FOREIGN KEY (venda_id)
        REFERENCES Tb_Venda (id),
    CONSTRAINT FK_MovEstoque_TipoMovimentacao       FOREIGN KEY (tipo_movimentacao_id)
        REFERENCES Tb_Tipo_Movimentacao (id),
    CONSTRAINT CHK_MovEstoque_Quantidade            CHECK (quantidade > 0)
);
GO

-- ================================================================
-- SEÇÃO 9 — ÍNDICES DE PERFORMANCE
-- ================================================================
-- Por que criar índices?
--   O SQL Server usa o índice clusterizado (PK) para buscas por ID.
--   Mas queries reais filtram por empresa_id, data, status etc.
--   Sem índices nessas colunas, o banco faz TABLE SCAN (lê tudo).
--   Com índices, faz INDEX SEEK (vai direto ao dado). Diferença: ms vs segundos.
--
-- Regra prática: índice em colunas que aparecem em WHERE, JOIN e ORDER BY.
-- Atenção: índice a mais também tem custo — deixa INSERT/UPDATE mais lento.
-- O conjunto abaixo é o mínimo necessário para um sistema comercial típico.

-- Usuários — login por username ou email
CREATE NONCLUSTERED INDEX IX_Usuario_EmpresaAtivo
    ON Tb_Usuario (empresa_id, ativo)
    INCLUDE (username, email, role);
GO

-- Funcionários — listagem e filtros por empresa
CREATE NONCLUSTERED INDEX IX_Funcionario_EmpresaAtivo
    ON Tb_Funcionario (empresa_id, ativo)
    INCLUDE (nome, cargo_id);
GO

-- Clientes — busca por nome (partial match) e CPF
CREATE NONCLUSTERED INDEX IX_Cliente_EmpresaAtivo
    ON Tb_Cliente (empresa_id, ativo)
    INCLUDE (nome, cpf, telefone);
GO

-- Produtos — listagem por categoria e status de estoque
CREATE NONCLUSTERED INDEX IX_Produto_EmpresaCategoria
    ON Tb_Produto (empresa_id, categoria_produto_id, ativo)
    INCLUDE (nome, preco_venda, quantidade_atual, estoque_minimo);
GO

-- Vendas — relatórios por período e funcionário
CREATE NONCLUSTERED INDEX IX_Venda_EmpresaDataVenda
    ON Tb_Venda (empresa_id, data_venda)
    INCLUDE (funcionario_id, cliente_id, valor_final);
GO

CREATE NONCLUSTERED INDEX IX_Venda_Funcionario
    ON Tb_Venda (funcionario_id, data_venda);
GO

CREATE NONCLUSTERED INDEX IX_Venda_Cliente
    ON Tb_Venda (cliente_id, data_venda);
GO

-- Itens de Venda — joins frequentes pelo id da venda
CREATE NONCLUSTERED INDEX IX_ItemVenda_Venda
    ON Tb_Item_Venda (venda_id)
    INCLUDE (produto_id, quantidade, preco_unitario, subtotal);
GO

-- Despesas — relatórios por período e categoria
CREATE NONCLUSTERED INDEX IX_Despesa_EmpresaData
    ON Tb_Despesa (empresa_id, data_despesa)
    INCLUDE (categoria_despesa_id, valor, fixa);
GO

-- Movimentação de Estoque — rastreabilidade por produto e período
CREATE NONCLUSTERED INDEX IX_MovEstoque_ProdutoData
    ON Tb_Movimentacao_Estoque (produto_id, data_movimentacao)
    INCLUDE (tipo_movimentacao_id, quantidade, quantidade_antes, quantidade_depois);
GO

-- Log — auditoria por usuário e data
CREATE NONCLUSTERED INDEX IX_LogSistema_EmpresaDataHora
    ON Tb_Log_Sistema (empresa_id, data_hora DESC)
    INCLUDE (usuario_id, acao, entidade_afetada, registro_id);
GO

-- ================================================================
-- SEÇÃO 10 — VIEWS UTILITÁRIAS
-- ================================================================
-- Views encapsulam queries complexas e recorrentes.
-- A aplicação consulta a view; o SQL fica centralizado no banco.
-- Mudou a lógica? Muda-se a view, não todos os lugares da aplicação.

-- View: produtos abaixo do estoque mínimo (alertas)
CREATE OR ALTER VIEW Vw_Produtos_Abaixo_Estoque_Minimo AS
    SELECT
        p.empresa_id,
        e.nome              AS empresa,
        p.id                AS produto_id,
        p.nome              AS produto,
        cp.nome             AS categoria,
        p.quantidade_atual,
        p.estoque_minimo,
        p.estoque_minimo - p.quantidade_atual AS quantidade_em_falta
    FROM Tb_Produto p
    INNER JOIN Tb_Empresa           e  ON e.id  = p.empresa_id
    INNER JOIN Tb_Categoria_Produto cp ON cp.id = p.categoria_produto_id
    WHERE
        p.ativo = 1
        AND p.estoque_minimo IS NOT NULL
        AND p.quantidade_atual < p.estoque_minimo;
GO

-- View: resumo de vendas por funcionário (comissão)
CREATE OR ALTER VIEW Vw_Resumo_Vendas_Funcionario AS
    SELECT
        v.empresa_id,
        v.funcionario_id,
        f.nome                              AS funcionario,
        c.nome                              AS cargo,
        CAST(v.data_venda AS DATE)          AS data_venda,
        COUNT(v.id)                         AS total_vendas,
        SUM(v.valor_final)                  AS valor_total,
        -- Comissão usa o percentual do funcionário; se null, usa o do cargo.
        SUM(v.valor_final)
            * COALESCE(f.per_comissao, c.per_comissao_base, 0) / 100  AS comissao_estimada
    FROM Tb_Venda       v
    INNER JOIN Tb_Funcionario   f ON f.id = v.funcionario_id
    INNER JOIN Tb_Cargo         c ON c.id = f.cargo_id
    GROUP BY
        v.empresa_id,
        v.funcionario_id,
        f.nome,
        c.nome,
        CAST(v.data_venda AS DATE),
        f.per_comissao,
        c.per_comissao_base;
GO

-- ================================================================
-- FIM DO SCRIPT
-- ================================================================
-- Próximos passos recomendados para um ambiente de produção:
--   1. Habilitar TDE (Transparent Data Encryption) se dados sensíveis.
--   2. Criar logins e roles específicos por módulo (princípio do menor privilégio).
--   3. Configurar JOB de backup diário (full) + log shipping.
--   4. Implementar Row-Level Security se a multi-tenancy exigir isolamento total.
--   5. Criar stored procedures para operações críticas (venda, movimentação de estoque).
-- ================================================================
