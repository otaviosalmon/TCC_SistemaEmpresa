USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'SistemaGestaoComercial')
BEGIN
    CREATE DATABASE SistemaGestaoComercial
    COLLATE Latin1_General_CI_AI;
END;
GO

USE SistemaGestaoComercial;
GO

CREATE TABLE Tb_Empresa (
    id          INT             NOT NULL IDENTITY(1,1),
    nome        VARCHAR(150)    NOT NULL,
    cnpj        VARCHAR(14)     NOT NULL,
    email       VARCHAR(150)        NULL,
    endereco    VARCHAR(200)        NULL,
    cidade      VARCHAR(100)        NULL,
    estado      CHAR(2)             NULL,
    cep         VARCHAR(10)         NULL,
    telefone    VARCHAR(20)         NULL,
    ativo       BIT             NOT NULL CONSTRAINT DF_Empresa_Ativo DEFAULT 1,

    CONSTRAINT PK_Empresa       PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_Empresa_CNPJ  UNIQUE (cnpj),
    CONSTRAINT CHK_Empresa_CNPJ CHECK (LEN(cnpj) = 14 AND cnpj NOT LIKE '%[^0-9]%')
);
GO

CREATE TABLE Tb_Categoria_Produto (
    id          INT             NOT NULL IDENTITY(1,1),
    empresa_id  INT             NOT NULL,
    nome        VARCHAR(150)    NOT NULL,
    descricao   VARCHAR(255)        NULL,
    ativo       BIT             NOT NULL CONSTRAINT DF_CategoriaProduto_Ativo DEFAULT 1,

    CONSTRAINT PK_CategoriaProduto          PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_CategoriaProduto_Empresa  FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id)
);
GO

CREATE TABLE Tb_Categoria_Despesa (
    id          INT             NOT NULL IDENTITY(1,1),
    empresa_id  INT             NOT NULL,
    nome        VARCHAR(100)    NOT NULL,
    descricao   VARCHAR(255)        NULL,
    ativo       BIT             NOT NULL CONSTRAINT DF_CategoriaDespesa_Ativo DEFAULT 1,

    CONSTRAINT PK_CategoriaDespesa          PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_CategoriaDespesa_Empresa  FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id)
);
GO

CREATE TABLE Tb_Forma_Pagamento (
    id          INT             NOT NULL IDENTITY(1,1),
    empresa_id  INT             NOT NULL,
    nome        VARCHAR(50)     NOT NULL,
    descricao   VARCHAR(150)        NULL,
    ativo       BIT             NOT NULL CONSTRAINT DF_FormaPagamento_Ativo DEFAULT 1,

    CONSTRAINT PK_FormaPagamento            PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_FormaPagamento_Empresa    FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id)
);
GO

CREATE TABLE Tb_Tipo_Movimentacao (
    id          INT             NOT NULL IDENTITY(1,1),
    empresa_id  INT             NOT NULL,
    nome        VARCHAR(100)    NOT NULL,
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

    CONSTRAINT PK_Cargo             PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Cargo_Empresa     FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT CHK_Cargo_Comissao   CHECK (per_comissao_base IS NULL
        OR (per_comissao_base >= 0 AND per_comissao_base <= 100))
);
GO

CREATE TABLE Tb_Usuario (
    id              INT             NOT NULL IDENTITY(1,1),
    empresa_id      INT             NOT NULL,
    username        VARCHAR(50)     NOT NULL,
    email           VARCHAR(150)    NOT NULL,
    password_hash   VARCHAR(255)    NOT NULL,
    role            VARCHAR(30)     NOT NULL,
    ativo           BIT             NOT NULL CONSTRAINT DF_Usuario_Ativo DEFAULT 1,
    data_cadastro   DATETIME        NOT NULL CONSTRAINT DF_Usuario_DataCadastro DEFAULT GETDATE(),

    CONSTRAINT PK_Usuario           PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Usuario_Empresa   FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT UQ_Usuario_Username  UNIQUE (username),
    CONSTRAINT UQ_Usuario_Email     UNIQUE (empresa_id, email),
    CONSTRAINT CHK_Usuario_Role     CHECK (role IN ('ADMIN', 'GERENTE', 'VENDEDOR', 'CAIXA', 'ESTOQUISTA'))
);
GO

CREATE TABLE Tb_Funcionario (
    id                  INT             NOT NULL IDENTITY(1,1),
    empresa_id          INT             NOT NULL,
    usuario_id          INT                 NULL,
    cargo_id            INT             NOT NULL,
    nome                VARCHAR(150)    NOT NULL,
    cpf                 VARCHAR(11)     NOT NULL,
    telefone            VARCHAR(20)         NULL,
    endereco            VARCHAR(200)        NULL,
    salario             DECIMAL(10,2)       NULL,
    per_comissao        DECIMAL(5,2)        NULL,
    data_admissao       DATE            NOT NULL,
    ativo               BIT             NOT NULL CONSTRAINT DF_Funcionario_Ativo DEFAULT 1,

    CONSTRAINT PK_Funcionario           PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Funcionario_Empresa   FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_Funcionario_Usuario   FOREIGN KEY (usuario_id)
        REFERENCES Tb_Usuario (id),
    CONSTRAINT FK_Funcionario_Cargo     FOREIGN KEY (cargo_id)
        REFERENCES Tb_Cargo (id),
    CONSTRAINT UQ_Funcionario_CPF       UNIQUE (empresa_id, cpf),
    CONSTRAINT CHK_Funcionario_CPF      CHECK (LEN(cpf) = 11 AND cpf NOT LIKE '%[^0-9]%'),
    CONSTRAINT CHK_Funcionario_Comissao CHECK (per_comissao IS NULL
        OR (per_comissao >= 0 AND per_comissao <= 100))
);
GO

CREATE TABLE Tb_Cliente (
    id              INT             NOT NULL IDENTITY(1,1),
    empresa_id      INT             NOT NULL,
    nome            VARCHAR(150)    NOT NULL,
    cpf             VARCHAR(11)         NULL,
    email           VARCHAR(150)        NULL,
    telefone        VARCHAR(20)         NULL,
    endereco        VARCHAR(255)        NULL,
    data_cadastro   DATETIME        NOT NULL CONSTRAINT DF_Cliente_DataCadastro DEFAULT GETDATE(),
    ativo           BIT             NOT NULL CONSTRAINT DF_Cliente_Ativo DEFAULT 1,

    CONSTRAINT PK_Cliente           PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Cliente_Empresa   FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT CHK_Cliente_CPF      CHECK (cpf IS NULL
        OR (LEN(cpf) = 11 AND cpf NOT LIKE '%[^0-9]%'))
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_Cliente_CPF
    ON Tb_Cliente (empresa_id, cpf)
    WHERE cpf IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_Cliente_Email
    ON Tb_Cliente (empresa_id, email)
    WHERE email IS NOT NULL;
GO

CREATE TABLE Tb_Produto (
    id                      INT             NOT NULL IDENTITY(1,1),
    empresa_id              INT             NOT NULL,
    categoria_produto_id    INT             NOT NULL,
    nome                    VARCHAR(150)    NOT NULL,
    descricao               VARCHAR(255)        NULL,
    preco_custo             DECIMAL(10,2)   NOT NULL,
    preco_venda             DECIMAL(10,2)   NOT NULL,
    quantidade_atual        INT             NOT NULL CONSTRAINT DF_Produto_QtdAtual DEFAULT 0,
    estoque_minimo          INT                 NULL,
    data_cadastro           DATETIME        NOT NULL CONSTRAINT DF_Produto_DataCadastro DEFAULT GETDATE(),
    ativo                   BIT             NOT NULL CONSTRAINT DF_Produto_Ativo DEFAULT 1,

    CONSTRAINT PK_Produto                   PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Produto_Empresa           FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_Produto_CategoriaProduto  FOREIGN KEY (categoria_produto_id)
        REFERENCES Tb_Categoria_Produto (id),
    CONSTRAINT CHK_Produto_Preco            CHECK (preco_venda >= preco_custo),
    CONSTRAINT CHK_Produto_QtdAtual         CHECK (quantidade_atual >= 0),
    CONSTRAINT CHK_Produto_EstoqueMinimo    CHECK (estoque_minimo IS NULL OR estoque_minimo >= 0)
);
GO

CREATE TABLE Tb_Venda (
    id                  INT             NOT NULL IDENTITY(1,1),
    empresa_id          INT             NOT NULL,
    funcionario_id      INT             NOT NULL,
    cliente_id          INT                 NULL,
    forma_pagamento_id  INT             NOT NULL,
    data_venda          DATETIME        NOT NULL CONSTRAINT DF_Venda_DataVenda DEFAULT GETDATE(),
    valor_total         DECIMAL(10,2)   NOT NULL,
    desconto            DECIMAL(10,2)   NOT NULL CONSTRAINT DF_Venda_Desconto DEFAULT 0,
    valor_final         DECIMAL(10,2)   NOT NULL,
    observacao          VARCHAR(255)        NULL,
    situacao_venda      VARCHAR(20)     NOT NULL CONSTRAINT DF_Venda_SituacaoVenda DEFAULT 'CONCLUIDA',

    CONSTRAINT PK_Venda                     PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Venda_Empresa             FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_Venda_Funcionario         FOREIGN KEY (funcionario_id)
        REFERENCES Tb_Funcionario (id),
    CONSTRAINT FK_Venda_Cliente             FOREIGN KEY (cliente_id)
        REFERENCES Tb_Cliente (id),
    CONSTRAINT FK_Venda_FormaPagamento      FOREIGN KEY (forma_pagamento_id)
        REFERENCES Tb_Forma_Pagamento (id),
    CONSTRAINT CHK_Venda_Desconto           CHECK (desconto >= 0),
    CONSTRAINT CHK_Venda_ValorFinal         CHECK (valor_final = valor_total - desconto),
    CONSTRAINT CHK_Venda_SituacaoVenda      CHECK (situacao_venda IN ('CONCLUIDA', 'CANCELADA'))
);
GO

CREATE TABLE Tb_Item_Venda (
    id              INT             NOT NULL IDENTITY(1,1),
    venda_id        INT             NOT NULL,
    produto_id      INT             NOT NULL,
    quantidade      INT             NOT NULL,
    preco_unitario  DECIMAL(10,2)   NOT NULL,
    preco_custo     DECIMAL(10,2)       NULL,
    subtotal        AS (quantidade * preco_unitario) PERSISTED,

    CONSTRAINT PK_ItemVenda                 PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_ItemVenda_Venda           FOREIGN KEY (venda_id)
        REFERENCES Tb_Venda (id),
    CONSTRAINT FK_ItemVenda_Produto         FOREIGN KEY (produto_id)
        REFERENCES Tb_Produto (id),
    CONSTRAINT CHK_ItemVenda_Quantidade     CHECK (quantidade > 0),
    CONSTRAINT CHK_ItemVenda_Preco          CHECK (preco_unitario > 0),
    CONSTRAINT CHK_ItemVenda_PrecoCusto     CHECK (preco_custo IS NULL OR preco_custo >= 0)
);
GO

CREATE TABLE Tb_Despesa (
    id                      INT             NOT NULL IDENTITY(1,1),
    empresa_id              INT             NOT NULL,
    categoria_despesa_id    INT             NOT NULL,
    usuario_id              INT             NOT NULL,
    descricao               VARCHAR(255)        NULL,
    valor                   DECIMAL(10,2)   NOT NULL,
    data_despesa            DATE            NOT NULL,
    fixa                    BIT             NOT NULL CONSTRAINT DF_Despesa_Fixa DEFAULT 0,
    observacao              VARCHAR(255)        NULL,

    CONSTRAINT PK_Despesa                   PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Despesa_Empresa           FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_Despesa_CategoriaDespesa  FOREIGN KEY (categoria_despesa_id)
        REFERENCES Tb_Categoria_Despesa (id),
    CONSTRAINT FK_Despesa_Usuario           FOREIGN KEY (usuario_id)
        REFERENCES Tb_Usuario (id),
    CONSTRAINT CHK_Despesa_Valor            CHECK (valor > 0)
);
GO

CREATE TABLE Tb_Log_Sistema (
    id                  BIGINT          NOT NULL IDENTITY(1,1),
    empresa_id          INT             NOT NULL,
    usuario_id          INT                 NULL,
    acao                VARCHAR(50)     NOT NULL,
    entidade_afetada    VARCHAR(100)    NOT NULL,
    registro_id         INT                 NULL,
    data_hora           DATETIME        NOT NULL CONSTRAINT DF_LogSistema_DataHora DEFAULT GETDATE(),
    detalhes            VARCHAR(MAX)        NULL,

    CONSTRAINT PK_LogSistema            PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_LogSistema_Empresa    FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_LogSistema_Usuario    FOREIGN KEY (usuario_id)
        REFERENCES Tb_Usuario (id)
);
GO

CREATE TABLE Tb_Movimentacao_Estoque (
    id                      INT             NOT NULL IDENTITY(1,1),
    empresa_id              INT             NOT NULL,
    produto_id              INT             NOT NULL,
    usuario_id              INT             NOT NULL,
    venda_id                INT                 NULL,
    tipo_movimentacao_id    INT             NOT NULL,
    quantidade              INT             NOT NULL,
    quantidade_antes        INT                 NULL,
    quantidade_depois       INT                 NULL,
    data_movimentacao       DATETIME        NOT NULL CONSTRAINT DF_MovEstoque_DataMovimentacao DEFAULT GETDATE(),
    observacao              VARCHAR(255)        NULL,

    CONSTRAINT PK_MovimentacaoEstoque           PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_MovEstoque_Empresa            FOREIGN KEY (empresa_id)
        REFERENCES Tb_Empresa (id),
    CONSTRAINT FK_MovEstoque_Produto            FOREIGN KEY (produto_id)
        REFERENCES Tb_Produto (id),
    CONSTRAINT FK_MovEstoque_Usuario            FOREIGN KEY (usuario_id)
        REFERENCES Tb_Usuario (id),
    CONSTRAINT FK_MovEstoque_Venda              FOREIGN KEY (venda_id)
        REFERENCES Tb_Venda (id),
    CONSTRAINT FK_MovEstoque_TipoMovimentacao   FOREIGN KEY (tipo_movimentacao_id)
        REFERENCES Tb_Tipo_Movimentacao (id),
    CONSTRAINT CHK_MovEstoque_Quantidade        CHECK (quantidade > 0)
);
GO

CREATE NONCLUSTERED INDEX IX_Usuario_EmpresaAtivo
    ON Tb_Usuario (empresa_id, ativo)
    INCLUDE (username, email, role);
GO

CREATE NONCLUSTERED INDEX IX_Funcionario_EmpresaAtivo
    ON Tb_Funcionario (empresa_id, ativo)
    INCLUDE (nome, cargo_id);
GO

CREATE NONCLUSTERED INDEX IX_Cliente_EmpresaAtivo
    ON Tb_Cliente (empresa_id, ativo)
    INCLUDE (nome, cpf, telefone);
GO

CREATE NONCLUSTERED INDEX IX_Produto_EmpresaCategoria
    ON Tb_Produto (empresa_id, categoria_produto_id, ativo)
    INCLUDE (nome, preco_venda, quantidade_atual, estoque_minimo);
GO

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

CREATE NONCLUSTERED INDEX IX_ItemVenda_Venda
    ON Tb_Item_Venda (venda_id)
    INCLUDE (produto_id, quantidade, preco_unitario, subtotal);
GO

CREATE NONCLUSTERED INDEX IX_Despesa_EmpresaData
    ON Tb_Despesa (empresa_id, data_despesa)
    INCLUDE (categoria_despesa_id, valor, fixa);
GO

CREATE NONCLUSTERED INDEX IX_MovEstoque_ProdutoData
    ON Tb_Movimentacao_Estoque (produto_id, data_movimentacao)
    INCLUDE (tipo_movimentacao_id, quantidade, quantidade_antes, quantidade_depois);
GO

CREATE NONCLUSTERED INDEX IX_LogSistema_EmpresaDataHora
    ON Tb_Log_Sistema (empresa_id, data_hora DESC)
    INCLUDE (usuario_id, acao, entidade_afetada, registro_id);
GO

CREATE OR ALTER VIEW Vw_Produtos_Abaixo_Estoque_Minimo AS
    SELECT
        p.empresa_id,
        e.nome                                  AS empresa,
        p.id                                    AS produto_id,
        p.nome                                  AS produto,
        cp.nome                                 AS categoria,
        p.quantidade_atual,
        p.estoque_minimo,
        p.estoque_minimo - p.quantidade_atual   AS quantidade_em_falta
    FROM Tb_Produto p
    INNER JOIN Tb_Empresa           e  ON e.id  = p.empresa_id
    INNER JOIN Tb_Categoria_Produto cp ON cp.id = p.categoria_produto_id
    WHERE
        p.ativo = 1
        AND p.estoque_minimo IS NOT NULL
        AND p.quantidade_atual < p.estoque_minimo;
GO

CREATE OR ALTER VIEW Vw_Resumo_Vendas_Funcionario AS
    SELECT
        v.empresa_id,
        v.funcionario_id,
        f.nome                              AS funcionario,
        c.nome                              AS cargo,
        CAST(v.data_venda AS DATE)          AS data_venda,
        COUNT(v.id)                         AS total_vendas,
        SUM(v.valor_final)                  AS valor_total,
        SUM(v.valor_final)
            * COALESCE(f.per_comissao, c.per_comissao_base, 0) / 100 AS comissao_estimada
    FROM Tb_Venda v
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
