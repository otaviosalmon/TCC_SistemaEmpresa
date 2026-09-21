# L.O. Solutions

**Sistema web de gestão comercial multiempresa para micro e pequenas empresas.**

ERP enxuto construído em ASP.NET Core MVC com SQL Server, cobrindo cadastros, estoque,
vendas, financeiro, relatórios e auditoria — mais uma camada analítica em Python que
projeta faturamento e despesas por regressão linear.

Trabalho de Conclusão de Curso — Bacharelado em Ciência da Computação, Uni-FACEF.

| | |
|---|---|
| **Autores** | Lucas Macedo Abrahão · Otávio Salomão |
| **Orientadora** | Prof.ª Dra. Silvia Regina Viel |
| **Instituição** | Centro Universitário Municipal de Franca (Uni-FACEF) |

> Este repositório concentra a **implementação**: código-fonte, scripts de banco e a
> documentação técnica das decisões de projeto. A fundamentação teórica, a elicitação de
> requisitos e a análise comparativa de ERPs de mercado ficam no artigo acadêmico.

---

## Sumário

- [Motivação](#motivação)
- [Stack](#stack)
- [Módulos implementados](#módulos-implementados)
- [Arquitetura](#arquitetura)
- [Estrutura do repositório](#estrutura-do-repositório)
- [Banco de dados](#banco-de-dados)
  - [Convenções do banco](#convenções-do-banco)
  - [Diagrama entidade-relacionamento](#diagrama-entidade-relacionamento)
  - [Dicionário do banco](#dicionário-do-banco)
  - [Views utilitárias](#views-utilitárias)
  - [Índices](#índices)
- [Padrões de código](#padrões-de-código)
  - [`ControllerValidacao` — controller base](#controllervalidacao--controller-base)
  - [`CpfAttribute` — validação de documento](#cpfattribute--validação-de-documento)
  - [`PasswordHasher` — hash de senha](#passwordhasher--hash-de-senha)
  - [`ClaimsEmpresa` — isolamento multiempresa](#claimsempresa--isolamento-multiempresa)
  - [Anatomia de um CRUD](#anatomia-de-um-crud)
  - [Verbos HTTP e rotas](#verbos-http-e-rotas)
  - [ViewModels](#viewmodels)
  - [`PaginacaoViewModel` — paginação reutilizável](#paginacaoviewmodel--paginação-reutilizável)
  - [Transações](#transações)
  - [Exclusão lógica e exclusão física](#exclusão-lógica-e-exclusão-física)
  - [Auditoria](#auditoria)
  - [Mapeamento EF Core](#mapeamento-ef-core)
  - [Camada de apresentação](#camada-de-apresentação)
- [Serviços de aplicação](#serviços-de-aplicação)
  - [`PrevisaoService` — HttpClient tipado](#previsaoservice--httpclient-tipado)
  - [`PlanilhaRelatorio` — exportação para Excel](#planilharelatorio--exportação-para-excel)
  - [`Aparencia` — preferências de interface](#aparencia--preferências-de-interface)
  - [`ExpiracaoLogBackgroundService` — retenção de log](#expiracaologbackgroundservice--retenção-de-log)
- [Camada analítica](#camada-analítica)
- [Requisitos e regras de negócio](#requisitos-e-regras-de-negócio)
  - [Requisitos funcionais](#requisitos-funcionais)
  - [Requisitos não funcionais](#requisitos-não-funcionais)
  - [Regras de negócio](#regras-de-negócio)
- [Perfis de acesso](#perfis-de-acesso)
- [Como executar](#como-executar)
- [Roadmap](#roadmap)
- [Autores](#autores)

---

## Motivação

Micro e pequenas empresas costumam operar com informação espalhada entre cadernos,
planilhas e sistemas isolados. O custo disso não é só organizacional: sem dado
centralizado e confiável, não há como apurar lucro real, antecipar ruptura de estoque
ou distinguir despesa recorrente de eventual.

O L.O. Solutions não tenta competir em quantidade de módulos com ERPs de mercado. A
proposta é reunir, sobre uma arquitetura simples e auditável, três frentes que raramente
aparecem juntas em soluções desse porte:

1. **Operação** — cadastros, estoque, vendas e financeiro integrados, com atualização
   automática de saldo, controle transacional e rastreabilidade completa.
2. **Análise** — dashboards de indicadores gerenciais e relatórios exportáveis
   construídos sobre os dados operacionais.
3. **Predição** — serviço em Python que projeta faturamento e despesas a partir do
   histórico mensal, consumido pela aplicação via HTTP.

A operação multiempresa é feita por **segregação lógica**: uma única base, com filtro por
`empresa_id` garantido estruturalmente no schema e reforçado em cada consulta.

---

## Stack

| Camada | Tecnologia | Versão |
|---|---|---|
| Linguagem | C# | 12 (`net8.0`) |
| Framework web | ASP.NET Core MVC | .NET 8 |
| ORM | Entity Framework Core | 8.0.29 (`Microsoft.EntityFrameworkCore.SqlServer`) |
| Banco de dados | SQL Server | 2019+ (desenvolvimento em SQLEXPRESS) |
| Autenticação | Cookie Authentication nativo | sem ASP.NET Identity — hash próprio |
| Exportação de planilha | ClosedXML | 0.105.1 |
| Camada analítica | Python + FastAPI + scikit-learn | FastAPI 0.115+, scikit-learn 1.6+ |
| Servidor da API analítica | Uvicorn | 0.34+ |
| Front-end | Razor Views + Bootstrap 5 | CSS próprio, temas claro/escuro |
| Validação client-side | jQuery Validation + Unobtrusive | mensagens em pt-BR |
| Gráficos | Chart.js 4 | vendorizado localmente |
| Modelagem do banco | BRModelo (lógico) + DDL manual | — |
| Versionamento | Git / GitHub | — |

### Abordagem de banco:

O schema é escrito à mão em `database/L.O_database.sql` e aplicado diretamente no SQL
Server. O `AppDbContext` mapeia as entidades C# para tabelas **já existentes**.

> **Este projeto não usa Migrations.** Executar `dotnet ef migrations add` geraria um
> histórico que não corresponde ao banco real e tentaria recriar tabelas existentes.
> Toda mudança de schema é feita diretamente no `database/L.O_database.sql`, mais o
> ajuste correspondente no Model e no `AppDbContext`.

> **Este projeto não usa Migrations.** Executar `dotnet ef migrations add` geraria um
> histórico que não corresponde ao banco real e tentaria recriar tabelas existentes.
> Toda mudança de schema é um `ALTER TABLE` versionado em `database/update_db.sql`, mais
> o ajuste correspondente no Model e no `AppDbContext`.

---

## Arquitetura

O sistema segue o padrão **Model-View-Controller** do ASP.NET Core. A regra de negócio fica
nos Controllers — decisão coerente com o escopo e com a arquitetura documentada no artigo.
A pasta `Services/` não é uma camada de negócio: guarda integrações externas e utilitários
transversais que não pertencem a nenhum controller específico.

```
┌────────────────────────────────────────────────────────────┐
│  Views (Razor)                                             │
│  Apresentação e coleta. Sem lógica de negócio.             │
│  Formulário compartilhado por Create/Edit/Details.         │
├────────────────────────────────────────────────────────────┤
│  Controllers                                               │
│  Validação, regra de negócio, controle transacional,       │
│  autorização por perfil e registro de auditoria.           │
│  Todos herdam de ControllerValidacao.                      │
├────────────────────────────────────────────────────────────┤
│  ViewModels                │  Services/                    │
│  Contrato Controller↔View  │  Integrações e utilitários:   │
│  Data Annotations,         │  PrevisaoService (HTTP),      │
│  paginação, dropdowns.     │  PlanilhaRelatorio (xlsx),    │
│                            │  Aparencia, BackgroundService │
├────────────────────────────────────────────────────────────┤
│  Models + AppDbContext (EF Core)                           │
│  Entidades de domínio e mapeamento objeto-relacional.      │
└───────────────┬────────────────────────────┬───────────────┘
                │                            │
       ┌────────▼────────┐          ┌────────▼─────────────┐
       │   SQL Server    │          │  API analítica       │
       │  15 tabelas     │          │  FastAPI + sklearn   │
       │  2 views        │          │  POST /previsao      │
       │  13 índices     │          │  regressão linear    │
       └─────────────────┘          └──────────────────────┘
```

### Decisões arquiteturais

| Decisão | Motivação |
|---|---|
| **Sem repositório genérico** | Acesso direto ao `DbContext` nos Controllers. Uma camada de repositório sobre o EF Core seria abstração redundante — o `DbSet<T>` já é um repositório. |
| **Sem camada de Services de negócio** | O escopo não justifica a indireção. A lógica compartilhada entre Controllers vive na classe base `ControllerValidacao`. |
| **ViewModels em vez de entidades nas Views** | Evita *over-posting*, permite validação específica de formulário e impede que propriedades de navegação vazem para o Razor. |
| **`AsNoTracking()` em toda leitura** | O change tracker só é necessário quando há intenção de gravar. Em listagem e consulta ele só custa memória e CPU. |
| **Constantes tipadas em vez de strings mágicas** | `SituacaoVenda.Concluida` em vez de `"CONCLUIDA"` transforma erro de digitação em erro de compilação. |
| **Camada analítica como serviço HTTP separado** | Python e .NET convivem sem acoplamento de processo. A aplicação degrada com elegância quando o serviço está fora do ar — a tela informa em vez de quebrar. |

---

## Estrutura do repositório

```
TCC_SistemaEmpresa/
├── Controllers/
│   ├── ControllerValidacao.cs          # Classe base abstrata dos controllers de módulo
│   ├── AccountController.cs            # Login, logout, acesso negado
│   ├── DashboardController.cs          # KPIs e gráficos
│   ├── PrevisaoController.cs           # Monta séries e consome a API analítica
│   ├── RelatoriosController.cs         # Relatórios em tela e exportação .xlsx
│   ├── RegistroController.cs           # Consulta ao log de auditoria (ADMIN)
│   ├── ConfiguracoesController.cs      # Perfil, dados da empresa e aparência
│   └── ...                             # Um controller por módulo CRUD
├── Data/
│   └── AppDbContext.cs                 # DbSets + OnModelCreating (mapeamento snake_case)
├── Models/
│   ├── *.cs                            # 15 entidades espelhando as tabelas
│   └── ViewModels/
│       ├── *FormViewModel.cs           # Contrato de formulário (Create/Edit/Details)
│       ├── *ListaViewModel.cs          # Contrato de listagem (filtros + linhas)
│       ├── PaginacaoViewModel.cs       # Paginação + métodos de extensão
│       ├── RelatorioViewModel.cs       # Relatório genérico (colunas, linhas, totais)
│       └── PrevisaoViewModel.cs        # Séries histórica e projetada
├── Security/
│   ├── PasswordHasher.cs               # PBKDF2-HMAC-SHA256
│   └── ClaimsEmpresa.cs                # Nomes das claims customizadas
├── Services/
│   ├── PrevisaoService.cs              # HttpClient tipado para a API Python
│   ├── PrevisaoContratos.cs            # DTOs do contrato JSON
│   ├── PlanilhaRelatorio.cs            # Geração de .xlsx com ClosedXML
│   ├── Aparencia.cs                    # Tema, fonte e densidade via cookie
│   └── ExpiracaoLogBackgroundService.cs # Retenção de log (12 meses)
├── Validation/
│   └── CpfAttribute.cs                 # ValidationAttribute customizado
├── Views/
│   ├── Shared/                         # _Layout, _Sidebar
│   └── <Modulo>/
│       ├── Index / Create / Edit / Details
│       └── _Formulario.cshtml          # Partial compartilhada
├── wwwroot/
│   ├── css/                            # app, sidebar, dashboard, login
│   ├── js/                             # módulos IIFE isolados
│   └── lib/                            # Bootstrap, jQuery, Chart.js (vendorizados)
└── Program.cs                          # Composition root: DI, auth, cultura, pipeline

analytics/
├── previsao_api.py                     # API FastAPI de previsão (regressão linear)
└── requirements.txt                    # Dependências Python

database/
├── L.O_database.sql                    # DDL completo — fonte de verdade do schema
└── seed_db.sql                         # Massa de dados: 2 empresas, 24 meses de histórico                         # Massa de dados

Models DB/
└── DER L.O Solutions.png               # Diagrama entidade-relacionamento
```

---

## Banco de dados

A modelagem foi desenvolvida para garantir organização, integridade, rastreabilidade e
consistência das informações. O modelo lógico foi elaborado na ferramenta **BRModelo** e
implementado em **SQL Server**, com DDL escrito manualmente.

A estrutura contempla estoque, vendas, despesas, clientes, funcionários, usuários e
auditoria em **15 tabelas**, **2 views** e **13 índices não-clusterizados**.

### Convenções do banco

| Item | Convenção |
|---|---|
| Nome de tabela | Prefixo `Tb_`, underscore entre palavras: `Tb_Item_Venda`, `Tb_Categoria_Produto` |
| Nome de coluna | `snake_case` minúsculo: `empresa_id`, `password_hash`, `quantidade_atual` |
| Chave primária | `id INT IDENTITY(1,1)`, clusterizada. Exceção: `Tb_Log_Sistema.id` é `BIGINT` |
| Chave estrangeira | `<entidade>_id`: `empresa_id`, `produto_id`, `categoria_produto_id` |
| Valores monetários | `DECIMAL(10,2)`. Percentual de comissão: `DECIMAL(5,2)` |
| Booleano | `BIT` com `DEFAULT` nomeado (`DF_<Tabela>_<Coluna>`) |
| Datas | `DATETIME` para timestamp; `DATE` quando só a data importa |
| Texto | `VARCHAR` |
| Collation | `Latin1_General_CI_AI` — *case* e *accent insensitive* |
| Constraints | Sempre nomeadas: `PK_`, `FK_`, `UQ_`, `IX_`, `CHK_`, `DF_` |


### Diagrama entidade-relacionamento

![Diagrama Entidade-Relacionamento do L.O. Solutions](Models%20DB/DER%20L.O%20Solutions.png)

O diagrama evidencia o eixo do multiempresa: **13 das 15 tabelas** têm `empresa_id NOT NULL`
apontando para `Tb_Empresa`. As duas exceções são a própria `Tb_Empresa`, que é a raiz, e
`Tb_Item_Venda`, que alcança a empresa indiretamente por `venda_id`.

### Dicionário do Banco

#### `Tb_Empresa` — raiz do multiempresa

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `nome` | VARCHAR(150) | NOT NULL | |
| `cnpj` | VARCHAR(14) | NOT NULL | sem máscara — `UQ_Empresa_CNPJ` + `CHK_Empresa_CNPJ` (14 dígitos) |
| `email` | VARCHAR(150) | NULL | |
| `endereco` | VARCHAR(200) | NULL | |
| `cidade` | VARCHAR(100) | NULL | |
| `estado` | CHAR(2) | NULL | UF — `CHAR` fixo |
| `cep` | VARCHAR(10) | NULL | |
| `telefone` | VARCHAR(20) | NULL | |
| `ativo` | BIT | NOT NULL | `DEFAULT 1` — exclusão lógica |

**Justificativa.** Necessária porque o sistema opera em cenário multiempresa, permitindo
que diferentes organizações usem a aplicação mantendo segregação lógica dos dados.

**Relacionamentos.** Relaciona-se com usuários, clientes, funcionários, produtos e demais
entidades operacionais.

**Cardinalidade.** 1:N com todas as outras 14 tabelas — uma empresa possui vários usuários,
produtos e vendas, porém cada registro pertence a exatamente uma empresa.

O CNPJ é gravado **sem máscara**, apenas dígitos. A formatação é responsabilidade da View;
a validação do dígito verificador é da aplicação. O banco garante somente o formato.

#### `Tb_Usuario` — autenticação e rastreabilidade

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `username` | VARCHAR(50) | NOT NULL | login — **único globalmente** |
| `email` | VARCHAR(150) | NOT NULL | |
| `password_hash` | VARCHAR(255) | NOT NULL | nunca texto claro |
| `role` | VARCHAR(30) | NOT NULL | perfil de acesso |
| `ativo` | BIT | NOT NULL | `DEFAULT 1` |
| `data_cadastro` | DATETIME | NOT NULL | `DEFAULT GETDATE()` |

**Constraints:** `UQ_Usuario_Username (username)` · `UQ_Usuario_Email (empresa_id, email)` ·
`CHK_Usuario_Role CHECK (role IN ('ADMIN','GERENTE','VENDEDOR','CAIXA','ESTOQUISTA'))`

**Índice:** `IX_Usuario_EmpresaAtivo (empresa_id, ativo) INCLUDE (username, email, role)`

**Justificativa.** Entidade necessária para autenticação, controle de acesso e
rastreabilidade das operações realizadas no sistema.

**Relacionamentos.** O usuário registra movimentações de estoque, lança despesas e gera
logs do sistema.

**Cardinalidade.** 1:N — um usuário pode realizar várias operações, porém cada operação
possui apenas um responsável, o que garante rastreabilidade e auditoria.

> **`username` é único em todo o sistema**, não por empresa. O schema original usava a
> chave composta `(empresa_id, username)`, mas o login acontece antes de o sistema saber a
> qual empresa o usuário pertence — com homônimos em empresas diferentes, a autenticação
> entraria numa empresa arbitrária.

#### `Tb_Log_Sistema` — auditoria

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | **BIGINT** IDENTITY | NOT NULL | PK — `BIGINT` porque log cresce muito |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `usuario_id` | INT | NULL | FK → `Tb_Usuario`. NULL = ação do sistema |
| `acao` | VARCHAR(50) | NOT NULL | operação executada |
| `entidade_afetada` | VARCHAR(100) | NOT NULL | nome da entidade |
| `registro_id` | INT | NULL | id do registro afetado |
| `data_hora` | DATETIME | NOT NULL | `DEFAULT GETDATE()` |
| `detalhes` | VARCHAR(MAX) | NULL | contexto em texto livre |

**Índice:** `IX_LogSistema_EmpresaDataHora (empresa_id, data_hora DESC) INCLUDE (usuario_id, acao, entidade_afetada, registro_id)`

**Justificativa.** Possibilita auditoria e rastreabilidade das operações críticas
realizadas pelos usuários.

**Relacionamentos.** Relaciona-se com `Tb_Usuario` e `Tb_Empresa`.

**Cardinalidade.** 1:N — um usuário pode gerar vários logs; cada log pertence a no máximo
um usuário.

O log é **polimórfico**: `entidade_afetada` + `registro_id` identificam o alvo sem FK
tipada para cada tabela de negócio. Isso permite auditar qualquer entidade sem alterar o
schema.

As FKs de log são deliberadamente **sem `ON DELETE CASCADE`**: o log sobrevive à remoção do
usuário ou da empresa. No model C#, `Id` é `long` — ler um `BIGINT` em `int` estoura
`InvalidCastException` no `SqlDataReader`.

A retenção é de 12 meses, aplicada por `ExpiracaoLogBackgroundService`.

#### `Tb_Cargo`

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `nome` | VARCHAR(100) | NOT NULL | |
| `descricao` | VARCHAR(255) | NULL | |
| `salario_base` | DECIMAL(10,2) | NULL | |
| `per_comissao_base` | DECIMAL(5,2) | NULL | `CHK_Cargo_Comissao`: NULL ou 0–100 |
| `ativo` | BIT | NOT NULL | `DEFAULT 1` |

**Justificativa.** A separação de cargos evita redundância de informações salariais e
permite reutilizar o mesmo cargo para vários funcionários.

**Relacionamentos.** Relaciona-se com `Tb_Funcionario`.

**Cardinalidade.** 1:N — um cargo pode estar associado a vários funcionários; um
funcionário possui apenas um cargo.

#### `Tb_Funcionario`

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `usuario_id` | INT | NULL | FK → `Tb_Usuario`. NULL = sem acesso ao sistema |
| `cargo_id` | INT | NOT NULL | FK → `Tb_Cargo` |
| `nome` | VARCHAR(150) | NOT NULL | |
| `cpf` | VARCHAR(11) | NOT NULL | sem máscara — `CHK_Funcionario_CPF`: 11 dígitos |
| `telefone` | VARCHAR(20) | NULL | |
| `endereco` | VARCHAR(200) | NULL | |
| `salario` | DECIMAL(10,2) | NULL | sobrescreve `Tb_Cargo.salario_base` |
| `per_comissao` | DECIMAL(5,2) | NULL | `CHK_Funcionario_Comissao`: NULL ou 0–100 |
| `data_admissao` | DATE | NOT NULL | `DATE`, não `DATETIME` |
| `ativo` | BIT | NOT NULL | `DEFAULT 1` |

**Constraints:** `UQ_Funcionario_CPF (empresa_id, cpf)` — CPF único **por empresa**

**Índice:** `IX_Funcionario_EmpresaAtivo (empresa_id, ativo) INCLUDE (nome, cargo_id)`

**Justificativa.** Permite controlar funcionários vinculados às operações comerciais.

**Relacionamentos.** O funcionário realiza vendas, possui cargo e pode estar associado a um
usuário do sistema.

**Cardinalidade.**
- **Funcionário × Venda — 1:N.** Um funcionário realiza várias vendas; cada venda é
  realizada por apenas um funcionário.
- **Funcionário × Usuário — 0:1.** Um funcionário pode possuir usuário, porém nem todo
  funcionário acessa o sistema.

> **Precedência da comissão:** o cargo define o padrão e o funcionário sobrescreve. A view
> `Vw_Resumo_Vendas_Funcionario` implementa isso com
> `COALESCE(f.per_comissao, c.per_comissao_base, 0)`, e a mesma precedência vale no C#.

#### `Tb_Cliente`

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `nome` | VARCHAR(150) | NOT NULL | |
| `cpf` | VARCHAR(11) | NULL | permite venda de balcão sem identificação |
| `email` | VARCHAR(150) | NULL | |
| `telefone` | VARCHAR(20) | NULL | |
| `endereco` | VARCHAR(255) | NULL | |
| `data_cadastro` | DATETIME | NOT NULL | `DEFAULT GETDATE()` |
| `ativo` | BIT | NOT NULL | `DEFAULT 1` |

**Constraints:** `CHK_Cliente_CPF` (NULL ou 11 dígitos)

**Índices:** `IX_Cliente_EmpresaAtivo (empresa_id, ativo) INCLUDE (nome, cpf, telefone)` ·
`UQ_Cliente_CPF` e `UQ_Cliente_Email` — **índices únicos filtrados**

**Justificativa.** Necessária para controle do histórico de compras e relacionamento
comercial.

**Relacionamentos.** Relaciona-se com vendas.

**Cardinalidade.** 1:N — um cliente pode realizar várias compras; cada venda pertence a no
máximo um cliente.

#### `Tb_Categoria_Produto`

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `nome` | VARCHAR(150) | NOT NULL | |
| `descricao` | VARCHAR(255) | NULL | |
| `ativo` | BIT | NOT NULL | `DEFAULT 1` — adicionado durante a implementação |

**Justificativa.** A categorização facilita organização, filtros e relatórios gerenciais.

**Relacionamentos.** Relaciona-se com produtos.

**Cardinalidade.** 1:N — uma categoria contém vários produtos; um produto pertence a apenas
uma categoria.

#### `Tb_Produto` — entidade central

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `categoria_produto_id` | INT | NOT NULL | FK → `Tb_Categoria_Produto` |
| `nome` | VARCHAR(150) | NOT NULL | |
| `descricao` | VARCHAR(255) | NULL | |
| `preco_custo` | DECIMAL(10,2) | NOT NULL | base do lucro bruto |
| `preco_venda` | DECIMAL(10,2) | NOT NULL | |
| `quantidade_atual` | INT | NOT NULL | `DEFAULT 0` — saldo denormalizado |
| `estoque_minimo` | INT | NULL | dispara alerta de reposição |
| `data_cadastro` | DATETIME | NOT NULL | `DEFAULT GETDATE()` |
| `ativo` | BIT | NOT NULL | `DEFAULT 1` — inativo não pode ser vendido |

**Constraints:** `CHK_Produto_Preco CHECK (preco_venda >= preco_custo)` ·
`CHK_Produto_QtdAtual CHECK (quantidade_atual >= 0)` ·
`CHK_Produto_EstoqueMinimo CHECK (estoque_minimo IS NULL OR estoque_minimo >= 0)`

**Índice:** `IX_Produto_EmpresaCategoria (empresa_id, categoria_produto_id, ativo) INCLUDE (nome, preco_venda, quantidade_atual, estoque_minimo)`

**Justificativa.** Entidade central para o funcionamento do estoque e das vendas.

**Relacionamentos.** Pertence a uma categoria, participa de itens de venda e possui
movimentações de estoque.

**Cardinalidade.**
- **Produto × ItemVenda — 1:N.** Um produto aparece em vários itens; cada item refere-se a
  um único produto.
- **Produto × MovimentacaoEstoque — 1:N.** Um produto possui várias movimentações; cada
  movimentação refere-se a um único produto.

> **Saldo denormalizado.** `quantidade_atual` duplica informação derivável de
> `Tb_Movimentacao_Estoque`. A denormalização é intencional: recalcular o saldo somando o
> histórico a cada listagem seria inviável em desempenho. O custo é que os dois **precisam**
> ser atualizados atomicamente — ver [Transações](#transações).
>
> **`CHK_Produto_QtdAtual` impede saldo negativo em qualquer circunstância**, inclusive para
> perfil administrativo. Na implementação atual, estoque insuficiente bloqueia a venda para
> todos os perfis.

#### `Tb_Tipo_Movimentacao`

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `nome` | VARCHAR(100) | NOT NULL | rótulo de negócio ("Baixa por venda") |
| `natureza` | VARCHAR(10) | NOT NULL | direção do estoque |
| `descricao` | VARCHAR(255) | NULL | |
| `ativo` | BIT | NOT NULL | `DEFAULT 1` |

**Constraint:** `CHK_TipoMovimentacao_Natureza CHECK (natureza IN ('ENTRADA', 'SAIDA'))`

**Justificativa.** A separação dos tipos evita repetição textual e padroniza operações de
estoque.

**Relacionamentos.** Relaciona-se com movimentações.

**Cardinalidade.** 1:N — um tipo classifica várias movimentações; cada movimentação possui
apenas um tipo.

> **A direção do estoque vem da `natureza`, não do sinal da quantidade.** Como
> `CHK_MovEstoque_Quantidade` exige `quantidade > 0` sempre, é o tipo que determina se a
> operação soma ou subtrai. O ajuste manual é modelado como **dois tipos** (`Ajuste de
> entrada` e `Ajuste de saída`), já que a natureza é fixa por tipo.

#### `Tb_Movimentacao_Estoque` — livro-razão do estoque

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `produto_id` | INT | NOT NULL | FK → `Tb_Produto` |
| `usuario_id` | INT | NOT NULL | FK → `Tb_Usuario` |
| `venda_id` | INT | NULL | FK → `Tb_Venda`. NULL = movimentação manual |
| `tipo_movimentacao_id` | INT | NOT NULL | FK → `Tb_Tipo_Movimentacao` |
| `quantidade` | INT | NOT NULL | `CHK_MovEstoque_Quantidade CHECK (quantidade > 0)` |
| `quantidade_antes` | INT | NULL | saldo antes da operação |
| `quantidade_depois` | INT | NULL | saldo depois da operação |
| `data_movimentacao` | DATETIME | NOT NULL | `DEFAULT GETDATE()` |
| `observacao` | VARCHAR(255) | NULL | |

**Índice:** `IX_MovEstoque_ProdutoData (produto_id, data_movimentacao) INCLUDE (tipo_movimentacao_id, quantidade, quantidade_antes, quantidade_depois)`

**Justificativa.** Permite rastrear alterações no estoque e manter histórico operacional.

**Relacionamentos.** Relaciona-se com produto, usuário, tipo de movimentação e —
opcionalmente — venda.

**Cardinalidade.** 1:N em todas as relações: um produto possui várias movimentações; um
usuário registra várias movimentações; um tipo classifica várias movimentações.

> **`venda_id` vincula a baixa à venda que a originou.** É o que torna o cancelamento
> exato: as movimentações a estornar são localizadas por `WHERE venda_id = @venda`, sem
> inferência por produto e data.
>
> **`quantidade_antes` e `quantidade_depois` são nullable no banco, mas a aplicação sempre
> preenche os dois.** Sem eles não é possível reconstruir a linha do tempo do saldo.

#### `Tb_Forma_Pagamento`

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `nome` | VARCHAR(50) | NOT NULL | |
| `descricao` | VARCHAR(150) | NULL | |
| `ativo` | BIT | NOT NULL | `DEFAULT 1` |

**Justificativa.** A separação das formas de pagamento permite padronização e maior
flexibilidade do sistema.

**Relacionamentos.** Relaciona-se com vendas.

**Cardinalidade.** 1:N — uma forma de pagamento aparece em várias vendas; cada venda utiliza
apenas uma forma principal.

Valores previstos: dinheiro, cartão, PIX, boleto e outros.

#### `Tb_Venda`

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `funcionario_id` | INT | NOT NULL | FK → `Tb_Funcionario` — quem realizou |
| `cliente_id` | INT | NULL | FK → `Tb_Cliente`. NULL = venda sem identificação |
| `forma_pagamento_id` | INT | NOT NULL | FK → `Tb_Forma_Pagamento` |
| `data_venda` | DATETIME | NOT NULL | `DEFAULT GETDATE()` |
| `valor_total` | DECIMAL(10,2) | NOT NULL | soma dos subtotais dos itens |
| `desconto` | DECIMAL(10,2) | NOT NULL | `DEFAULT 0` |
| `valor_final` | DECIMAL(10,2) | NOT NULL | |
| `observacao` | VARCHAR(255) | NULL | |
| `situacao_venda` | VARCHAR(20) | NOT NULL | `DEFAULT 'CONCLUIDA'` — acréscimo da implementação |

**Constraints:** `CHK_Venda_Desconto CHECK (desconto >= 0)` ·
`CHK_Venda_ValorFinal CHECK (valor_final = valor_total - desconto)` ·
`CHK_Venda_SituacaoVenda CHECK (situacao_venda IN ('CONCLUIDA','CANCELADA'))`

**Índices:** `IX_Venda_EmpresaDataVenda (empresa_id, data_venda) INCLUDE (funcionario_id, cliente_id, valor_final)` ·
`IX_Venda_Funcionario (funcionario_id, data_venda)` · `IX_Venda_Cliente (cliente_id, data_venda)`

**Justificativa.** Centraliza as informações gerais da venda.

**Relacionamentos.** Relaciona-se com cliente, funcionário, forma de pagamento e itens.

**Cardinalidade.**
- **Venda × ItemVenda — 1:N.** Uma venda possui vários itens; um item pertence a apenas uma
  venda.
- **Cliente × Venda — 1:N.** Um cliente realiza várias compras; cada venda pertence a um
  único cliente (ou a nenhum, na venda de balcão).

> **`CHK_Venda_ValorFinal` valida a aritmética no próprio banco.** Se o cálculo em C#
> divergir por arredondamento ou erro de lógica, o INSERT falha e a venda inteira é
> rejeitada — não há como gravar cabeçalho inconsistente.
>
> **Toda apuração financeira filtra `situacao_venda = 'CONCLUIDA'`.** Sem esse filtro,
> vendas canceladas voltariam a compor o faturamento, o dashboard e a previsão.

#### `Tb_Item_Venda` — resolve o N:N entre venda e produto

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `venda_id` | INT | NOT NULL | FK → `Tb_Venda` |
| `produto_id` | INT | NOT NULL | FK → `Tb_Produto` |
| `quantidade` | INT | NOT NULL | `CHK_ItemVenda_Quantidade CHECK (quantidade > 0)` |
| `preco_unitario` | DECIMAL(10,2) | NOT NULL | snapshot do preço de venda |
| `preco_custo` | DECIMAL(10,2) | NULL | snapshot do custo — acréscimo da implementação |
| `subtotal` | COMPUTED PERSISTED | — | `AS (quantidade * preco_unitario) PERSISTED` |

**Constraints:** `CHK_ItemVenda_Preco CHECK (preco_unitario > 0)` ·
`CHK_ItemVenda_PrecoCusto CHECK (preco_custo IS NULL OR preco_custo >= 0)`

**Índice:** `IX_ItemVenda_Venda (venda_id) INCLUDE (produto_id, quantidade, preco_unitario, subtotal)`

**Justificativa.** A separação dos itens evita redundância e permite que uma venda contenha
vários produtos.

**Relacionamentos.** Relaciona-se com venda e produto.

**Cardinalidade.** Resolve uma relação **N:N** entre vendas e produtos — uma venda possui
vários produtos e um produto aparece em várias vendas. Por isso a entidade intermediária é
necessária.

> **`subtotal` é coluna calculada — o banco a produz, a aplicação nunca a grava.** Qualquer
> INSERT ou UPDATE que tente escrever nela falha. No EF Core a propriedade é marcada com
> `.ValueGeneratedOnAddOrUpdate()`.
>
> **É a única tabela sem `empresa_id`.** A empresa é alcançada por
> `venda_id → Tb_Venda.empresa_id`, portanto toda consulta a itens exige *join* com
> `Tb_Venda`. É o ponto do schema com maior risco de vazamento entre empresas, e por isso o
> *join* é aplicado mesmo onde pareceria redundante.
>
> **Não há desconto por item** — o desconto existe apenas no cabeçalho da venda.

#### `Tb_Categoria_Despesa`

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `nome` | VARCHAR(100) | NOT NULL | |
| `descricao` | VARCHAR(255) | NULL | |
| `ativo` | BIT | NOT NULL | `DEFAULT 1` |

**Justificativa.** Permite classificar despesas e gerar relatórios financeiros organizados.

**Relacionamentos.** Relaciona-se com despesas.

**Cardinalidade.** 1:N — uma categoria classifica várias despesas; cada despesa pertence a
apenas uma categoria.

#### `Tb_Despesa`

| Coluna | Tipo | Null | Observações |
|---|---|---|---|
| `id` | INT IDENTITY | NOT NULL | PK |
| `empresa_id` | INT | NOT NULL | FK → `Tb_Empresa` |
| `categoria_despesa_id` | INT | NOT NULL | FK → `Tb_Categoria_Despesa` |
| `usuario_id` | INT | NOT NULL | FK → `Tb_Usuario` — responsável pelo lançamento |
| `descricao` | VARCHAR(255) | NULL | |
| `valor` | DECIMAL(10,2) | NOT NULL | `CHK_Despesa_Valor CHECK (valor > 0)` |
| `data_despesa` | DATE | NOT NULL | `DATE`, não `DATETIME` |
| `fixa` | BIT | NOT NULL | `DEFAULT 0` — despesa recorrente |
| `observacao` | VARCHAR(255) | NULL | |

**Índice:** `IX_Despesa_EmpresaData (empresa_id, data_despesa) INCLUDE (categoria_despesa_id, valor, fixa)`

**Justificativa.** Necessária para controle financeiro e análise de gastos da empresa.

**Relacionamentos.** Relaciona-se com categoria de despesa e com o usuário responsável.

**Cardinalidade.**
- **CategoriaDespesa × Despesa — 1:N.** Uma categoria possui várias despesas; cada despesa
  pertence a uma única categoria.
- **Usuario × Despesa — 1:N.** Um usuário registra várias despesas; cada despesa possui
  apenas um responsável.

> **A coluna `fixa` separa despesa recorrente de eventual.** É insumo direto para a
> previsão: despesa fixa é projetável linearmente, despesa eventual exige tratamento
> estatístico distinto.

### Views utilitárias

| View | Entrega |
|---|---|
| `Vw_Produtos_Abaixo_Estoque_Minimo` | `empresa_id`, `empresa`, `produto_id`, `produto`, `categoria`, `quantidade_atual`, `estoque_minimo`, `quantidade_em_falta`. Filtra `ativo = 1` e `estoque_minimo IS NOT NULL`, com critério `quantidade_atual < estoque_minimo` |
| `Vw_Resumo_Vendas_Funcionario` | `empresa_id`, `funcionario_id`, `funcionario`, `cargo`, `data_venda`, `total_vendas`, `valor_total`, `comissao_estimada` |

> Ambas expõem `empresa_id` e **continuam exigindo filtro por empresa na aplicação**.
> Uma view não é um limite de segurança.

### Índices

Treze índices não-clusterizados, incluindo os dois índices únicos filtrados de
`Tb_Cliente`. O padrão dominante é `(empresa_id, <coluna de filtro>)` com `INCLUDE` das
colunas de listagem.

O efeito prático é que as telas de listagem por empresa são atendidas por *covering index*
— o SQL Server responde a consulta lendo só o índice, sem voltar à tabela base.

## Padrões de código

Esta seção documenta os mecanismos reutilizáveis do projeto: o que cada um resolve, por que
existe e onde é aplicado.

### `ControllerValidacao` — controller base

Classe **abstrata** da qual todos os controllers de módulo herdam. Concentra o que se
repetiria em cada um deles.

```csharp
public abstract class ControllerValidacao : Controller
{
    protected readonly AppDbContext _context;

    protected ControllerValidacao(AppDbContext context) => _context = context;

    protected abstract string EntidadeLog { get; }

    protected int EmpresaIdAtual()
    {
        var claim = User.FindFirstValue(Security.ClaimsEmpresa.EmpresaId);
        return int.TryParse(claim, out var empresaId) ? empresaId : 0;
    }

    protected int? UsuarioIdAtual()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var usuarioId) ? usuarioId : null;
    }

    protected void RegistrarLog(string acao, int registroId, string detalhes)
    {
        _context.LogsSistema.Add(new LogSistema
        {
            EmpresaId = EmpresaIdAtual(),
            UsuarioId = UsuarioIdAtual(),
            Acao = acao,
            EntidadeAfetada = EntidadeLog,
            RegistroId = registroId,
            DataHora = DateTime.Now,
            Detalhes = detalhes
        });
    }
}
```

| Membro | Responsabilidade |
|---|---|
| `_context` | `AppDbContext` injetado. `protected` — acessível às classes filhas, invisível de fora. |
| `EntidadeLog` | Propriedade **abstrata**. Cada controller declara o que está auditando (`protected override string EntidadeLog => nameof(Produto);`). Sendo abstrata, o compilador obriga toda classe filha a implementá-la — não há como esquecer. |
| `EmpresaIdAtual()` | Lê a claim `EmpresaId` do cookie. **Base do isolamento multiempresa**: toda consulta filtra por ele. |
| `UsuarioIdAtual()` | Lê `ClaimTypes.NameIdentifier`. Retorna `int?` — `null` quando não há sessão identificável, tratado como erro de validação em vez de exceção. |
| `RegistrarLog()` | Adiciona um `LogSistema` ao change tracker preenchendo empresa, usuário, data e entidade automaticamente. **Não chama `SaveChanges()`** — o log entra no mesmo `SaveChanges()` da operação auditada, então ou os dois gravam ou nenhum grava. |

**Por que classe base e não serviço injetado:** o acesso ao `User` (claims) e ao `ModelState`
é natural dentro do `Controller`. Extrair isso para um serviço exigiria passar o
`HttpContext` adiante, o que é mais indireção do que ganho.

### `CpfAttribute` — validação de documento

`ValidationAttribute` próprio que valida o dígito verificador do CPF. Aplicado por decoração
no ViewModel:

```csharp
[Cpf]
[Display(Name = "CPF")]
public string? Cpf { get; set; }
```

O ASP.NET Core executa todo `ValidationAttribute` durante o *model binding*, antes da action
rodar. Se `IsValid()` retorna `false`, o erro entra no `ModelState` com a mensagem definida
no construtor.

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class CpfAttribute : ValidationAttribute
{
    public CpfAttribute() : base("O CPF informado é inválido.") { }

    public override bool IsValid(object? value)
    {
        var texto = value as string;

        if (string.IsNullOrWhiteSpace(texto))
            return true;          // vazio é responsabilidade do [Required], não deste atributo

        return EhValido(texto);
    }

    public static string ApenasDigitos(string? texto) =>
        string.IsNullOrEmpty(texto)
            ? string.Empty
            : new string(texto.Where(char.IsDigit).ToArray());

    public static bool EhValido(string? texto)
    {
        var cpf = ApenasDigitos(texto);

        if (cpf.Length != 11)                       return false;
        if (cpf.All(digito => digito == cpf[0]))    return false;   // 111.111.111-11 e afins

        var numeros = cpf.Select(caractere => caractere - '0').ToArray();

        if (numeros[9] != CalcularDigito(numeros, 9))
            return false;

        return numeros[10] == CalcularDigito(numeros, 10);
    }

    private static int CalcularDigito(int[] numeros, int quantidade)
    {
        var soma = 0;
        var peso = quantidade + 1;

        for (var indice = 0; indice < quantidade; indice++)
            soma += numeros[indice] * (peso - indice);

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
```

Pontos de projeto:

- **Campo vazio retorna `true`.** Obrigatoriedade e formato são preocupações separadas: quem
  exige preenchimento é `[Required]`. Sem essa separação, um CPF opcional — caso de
  `Tb_Cliente`, que permite venda de balcão — seria impossível.
- **`ApenasDigitos()` é `public static`.** Além do uso interno, é reaproveitado pelos
  controllers para normalizar o valor antes de gravar: o banco armazena CPF **sem máscara**.
- **Rejeita sequências repetidas.** `111.111.111-11` passa no cálculo matemático do dígito
  verificador, mas é inválido na prática.
- **Um método para os dois dígitos.** `CalcularDigito()` é parametrizado pela quantidade de
  posições, evitando duplicar a mesma lógica com pesos diferentes.
- **`sealed`.** O atributo não foi projetado para herança; selar deixa isso explícito e
  permite otimização do JIT.

A máscara visual é aplicada no cliente por `wwwroot/js/mascara-cpf.js`, que formata a
exibição sem alterar o valor enviado.

### `PasswordHasher` — hash de senha

Classe estática responsável por gerar e verificar o hash gravado em
`Tb_Usuario.password_hash`.

**Algoritmo:** PBKDF2-HMAC-SHA256, 210.000 iterações (recomendação OWASP), saída de 32 bytes.

**Formato persistido:**

```
PBKDF2-SHA256$<iteracoes>$<hash base64>
```

As iterações ficam gravadas dentro da própria string. Isso permite aumentar o custo do KDF
no futuro sem invalidar os hashes existentes — cada hash carrega o parâmetro com que foi
gerado.

| Aspecto | Implementação |
|---|---|
| **Salt derivado do username** | O salt é `SHA256("LOSolutions.v1\|" + username normalizado)`. Torna o hash determinístico — o mesmo par (usuário, senha) sempre produz o mesmo resultado, o que permite gerar `INSERT` de teste direto no banco. Dois usuários com a mesma senha continuam tendo hashes diferentes. |
| **Normalização do username** | `Trim().ToLowerInvariant()` antes de derivar o salt. O banco usa collation `Latin1_General_CI_AI`: `'Teste'` e `'TESTE'` são o **mesmo** usuário para o SQL Server. Sem normalizar, logar com outra caixa geraria salt diferente e recusaria a senha correta. |
| **Prefixo de contexto no salt** | `"LOSolutions.v1\|"` impede que um hash gerado aqui colida com o de outro sistema que também use username como salt. |
| **Comparação em tempo fixo** | `CryptographicOperations.FixedTimeEquals()` em vez de `==`. Uma comparação comum retorna assim que encontra o primeiro byte diferente, e essa diferença de tempo vaza quantos bytes iniciais estavam corretos. |
| **Nunca lança exceção** | `Verificar()` retorna `false` para entrada nula, hash malformado ou base64 inválido. Uma exceção em fluxo de login viraria erro 500 e revelaria informação ao atacante. |
| **Compatibilidade retroativa** | Aceita também o formato legado `PBKDF2$sha256$<it>$<salt>$<hash>`. A contagem de segmentos após `Split('$')` distingue os formatos: 3 é o atual, 5 é o legado. Hashes novos saem sempre no formato atual. |

```csharp
var partes = hashArmazenado.Split('$');

return partes.Length switch
{
    3 => VerificarFormatoAtual(username, senha, partes),   // salt = username
    5 => VerificarFormatoLegado(senha, partes),            // salt aleatório embutido
    _ => false
};
```

> Senha em texto claro nunca é persistida nem registrada em log.

### `ClaimsEmpresa` — isolamento multiempresa

O requisito mais crítico do sistema: **um usuário só enxerga dados da própria empresa**. A
implementação combina três camadas.

**1. Estrutural — no banco.** 13 das 15 tabelas têm `empresa_id INT NOT NULL` com FK para
`Tb_Empresa`.

**2. Na sessão.** O `AccountController` grava `EmpresaId` como claim no cookie no momento do
login. O valor vem do banco, não de input do usuário.

```csharp
public static class ClaimsEmpresa
{
    public const string EmpresaId   = "EmpresaId";
    public const string EmpresaNome = "EmpresaNome";
}
```

`EmpresaId` é a claim de **segurança** — alimenta o `EmpresaIdAtual()` e, por consequência,
todo o isolamento. `EmpresaNome` é de **exibição**, usada pela sidebar e pela tela de
configurações. A separação é deliberada: nenhuma decisão de autorização depende do nome.

**3. Em cada consulta.** Todo acesso a entidade de negócio filtra pelo `EmpresaIdAtual()`:

```csharp
var empresaId = EmpresaIdAtual();

var consulta = _context.Produtos
    .AsNoTracking()
    .Where(p => p.EmpresaId == empresaId);
```

O padrão vale também para `Details`, `Edit` e `Excluir` — buscar por `id` sozinho permitiria
acessar o registro de outra empresa apenas trocando o número na URL:

```csharp
private Task<Produto?> BuscarDaEmpresaAsync(int id, bool rastrear)
{
    var empresaId = EmpresaIdAtual();

    var consulta = rastrear
        ? _context.Produtos.AsTracking()
        : _context.Produtos.AsNoTracking();

    return consulta.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
}
```

**Caso especial — `Tb_Item_Venda`.** É a única tabela sem `empresa_id`. Consultas a itens
exigem *join* com `Tb_Venda`:

```csharp
var custoProdutos = await _context.ItensVenda
    .AsNoTracking()
    .Where(i => i.Venda.EmpresaId == empresaId
             && i.Venda.SituacaoVenda == SituacaoVenda.Concluida)
    .SumAsync(i => (decimal?)(i.PrecoCusto ?? i.Produto.PrecoCusto * i.Quantidade)) ?? 0m;
```

### Anatomia de um CRUD

Todos os módulos seguem a mesma estrutura de actions. Usando `ClientesController` como
referência:

| Action | Verbo | Rota | Papel |
|---|---|---|---|
| `Index` | GET | `/Clientes` | listagem com busca, filtro e paginação |
| `Details` | GET | `/Clientes/Details/5` | visualização somente leitura |
| `Create` | GET | `/Clientes/Create` | formulário vazio |
| `Create` | POST | `/Clientes/Create` | valida, grava, loga, redireciona |
| `Edit` | GET | `/Clientes/Edit/5` | formulário preenchido |
| `Edit` | POST | `/Clientes/Edit/5` | valida, atualiza, loga, redireciona |
| `Excluir` | POST | `/Clientes/Excluir/5` | exclusão física com pré-condições |

**Listagem — filtros compostos com `switch expression`:**

```csharp
consulta = situacao switch
{
    SituacaoFiltro.Ativos   => consulta.Where(p => p.Ativo),
    SituacaoFiltro.Inativos => consulta.Where(p => !p.Ativo),
    _                       => consulta
};

if (!string.IsNullOrWhiteSpace(busca))
{
    var termo = busca.Trim();
    consulta = consulta.Where(p => p.Nome.Contains(termo));
}
```

Como `IQueryable` é composição preguiçosa, os filtros se acumulam sem tocar no banco. A
consulta só é executada no `ToListAsync()` final, e o SQL gerado já contém todos os `WHERE`.

**Projeção direta para ViewModel** — evita trazer a entidade inteira:

```csharp
var produtos = await consulta
    .OrderBy(p => p.Nome)
    .Select(p => new ProdutoLinhaViewModel
    {
        Id = p.Id,
        Nome = p.Nome,
        Categoria = p.CategoriaProduto.Nome,   // vira JOIN no SQL, não consulta extra
        PrecoVenda = p.PrecoVenda,
        QuantidadeAtual = p.QuantidadeAtual,
        Ativo = p.Ativo
    })
    .ToListAsync();
```

**Contadores agregados em uma consulta** — evita o problema N+1:

```csharp
var itensPorProduto = await _context.ItensVenda
    .AsNoTracking()
    .Where(i => ids.Contains(i.ProdutoId) && i.Venda.EmpresaId == empresaId)
    .GroupBy(i => i.ProdutoId)
    .Select(grupo => new { ProdutoId = grupo.Key, Total = grupo.Count() })
    .ToDictionaryAsync(x => x.ProdutoId, x => x.Total);
```

Uma consulta agregada para toda a página, em vez de uma por linha.

**Criação com transação e log:**

```csharp
await using var transacao = await _context.Database.BeginTransactionAsync();

_context.Produtos.Add(produto);
await _context.SaveChangesAsync();

if (estoqueInicial > 0)
{
    _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque { ... });
}

RegistrarLog("CRIACAO", produto.Id,
    $"Produto '{produto.Nome}' criado (estoque inicial {estoqueInicial}).");

await _context.SaveChangesAsync();
await transacao.CommitAsync();

TempData["Sucesso"] = $"Produto {produto.Nome} cadastrado com sucesso.";
return RedirectToAction(nameof(Index));
```

**Edição — a ação de log é derivada da mudança de estado**, não informada pelo formulário:

```csharp
var (acao, detalhe) = (estavaAtivo, model.Ativo) switch
{
    (true,  false) => ("INATIVACAO", "inativado"),
    (false, true)  => ("REATIVACAO", "reativado"),
    _              => ("ALTERACAO",  "alterado")
};

RegistrarLog(acao, produto.Id, $"Produto '{produto.Nome}' {detalhe}.");
```

**Falha de validação — recarregar o que não veio no POST:**

```csharp
if (!ModelState.IsValid)
{
    model.ProximoId  = await ProximoIdAsync();
    model.Categorias = await CarregarCategoriasAsync(model.CategoriaProdutoId);
    return View(model);   // devolve o formulário com os dados digitados preservados
}
```

As `SelectList` não fazem parte do POST; sem repopulá-las, o formulário volta com dropdowns
vazios.

**Revalidação de vínculo no servidor.** Filtrar o dropdown na tela não impede que alguém
altere o `value` no navegador:

```csharp
var categoriaValida = await _context.CategoriaProdutos
    .AsNoTracking()
    .AnyAsync(c => c.Id == model.CategoriaProdutoId && c.EmpresaId == empresaId);

if (!categoriaValida)
    ModelState.AddModelError(nameof(model.CategoriaProdutoId), "Tipo de produto inválido.");
```

**Previsão do próximo Id** — informativa, consultando o catálogo do SQL Server:

```csharp
const string sql = @"
    SELECT CASE
               WHEN coluna.last_value IS NULL THEN CONVERT(int, coluna.seed_value)
               ELSE CONVERT(int, coluna.last_value) + CONVERT(int, coluna.increment_value)
           END AS Value
      FROM sys.identity_columns AS coluna
     WHERE coluna.object_id = OBJECT_ID('Tb_Produto')";

try
{
    return await _context.Database.SqlQueryRaw<int>(sql).SingleOrDefaultAsync();
}
catch (Exception excecao)
{
    _logger.LogWarning(excecao, "Não foi possível prever o próximo id de Tb_Produto.");
    return null;
}
```

Em ambiente concorrente o valor pode não coincidir com o Id efetivamente atribuído, por isso
a falha é degradada para `null` — a View omite o campo em vez de quebrar.

### Verbos HTTP e rotas

Convenções aplicadas em todas as actions:

- **`[ValidateAntiForgeryToken]` em todo POST.** O token anti-CSRF é emitido pelo Tag Helper
  `asp-action` no formulário e validado no servidor. Sem ele, um site externo poderia
  submeter formulários em nome de um usuário autenticado.
- **Post/Redirect/Get.** Sucesso nunca retorna View diretamente — retorna
  `RedirectToAction`. Impede que um F5 reenvie o formulário e duplique o registro.
- **`TempData["Sucesso"]` / `TempData["Erro"]`.** Mensagens que sobrevivem exatamente a um
  redirect. O `_Layout` renderiza o alerta correspondente.
- **Actions destrutivas nunca em `GET`.** Exclusão e cancelamento são `POST`, o que impede
  que um crawler ou um link acidental dispare a operação.
- **`[FromRoute] int id`** explícito em actions com parâmetro de rota, deixando a origem do
  binding sem ambiguidade.
- **`[Authorize(Roles = ...)]` no nível da classe**, com restrição adicional na action
  quando necessário — é o caso de `VendasController.Cancelar`, restrito a `ADMIN` dentro de
  um controller aberto a quatro perfis.

### ViewModels

Cada módulo tem dois ViewModels com papéis distintos.

**`<Entidade>FormViewModel`** — contrato de formulário, compartilhado por `Create`, `Edit` e
`Details`.

```csharp
[BindNever]
public int Id { get; set; }

[BindNever]
public int? ProximoId { get; set; }

[Required(ErrorMessage = "Informe o nome do cliente.")]
[StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
[Display(Name = "Nome Cliente")]
public string Nome { get; set; } = string.Empty;

[Cpf]
[Display(Name = "CPF")]
public string? Cpf { get; set; }

public bool SomenteLeitura { get; set; }
public bool EhEdicao => Id > 0;
```

| Recurso | Função |
|---|---|
| `[BindNever]` | Impede que o model binder preencha a propriedade a partir do POST. Aplicado a `Id`, `ProximoId`, `DataCadastro` e contadores — campos que o servidor controla e que o usuário não pode forjar via *form tampering*. |
| `[Display(Name = ...)]` | Rótulo renderizado por `asp-for`. Concentra o texto da interface no ViewModel, evitando divergência entre telas. |
| Mensagens de erro explícitas | Todo atributo de validação define `ErrorMessage` em português. As mensagens padrão do framework vêm em inglês. |
| `SomenteLeitura` | Alterna o formulário para modo de consulta, permitindo que `Details` reutilize a mesma partial de `Create` e `Edit`. |
| `EhEdicao` | Propriedade calculada que a View usa para decidir título, rota de submit e visibilidade de campos. |
| Tipos anuláveis em campos numéricos | `decimal?` em vez de `decimal`. Sem isso, campo vazio vira `0` silenciosamente e a mensagem de obrigatoriedade nunca dispara. |

**`<Entidade>ListaViewModel`** — carrega os filtros ativos (para reexibi-los preenchidos), a
paginação e a coleção de linhas:

```csharp
public class VendaListaViewModel
{
    public string? Busca { get; set; }
    public DateTime? DataInicial { get; set; }
    public DateTime? DataFinal { get; set; }
    public string Filtro { get; set; } = FiltroVenda.Todas;
    public IReadOnlyList<VendaLinhaViewModel> Vendas { get; set; }
        = Array.Empty<VendaLinhaViewModel>();
}
```

`IReadOnlyList<T>` inicializado com `Array.Empty<T>()` comunica que a View não deve modificar
a coleção e elimina risco de `NullReferenceException` no Razor.

**Classes estáticas de constantes.** Valores que precisam coincidir exatamente com `CHECK
constraints` do banco ou com parâmetros de query string:

```csharp
public static class SituacaoVenda        // espelha CHK_Venda_SituacaoVenda
{
    public const string Concluida = "CONCLUIDA";
    public const string Cancelada = "CANCELADA";
}

public static class RolesUsuario         // espelha CHK_Usuario_Role
{
    public const string Admin      = "ADMIN";
    public const string Gerente    = "GERENTE";
    public const string Vendedor   = "VENDEDOR";
    public const string Caixa      = "CAIXA";
    public const string Estoquista = "ESTOQUISTA";

    public const string Todos = "todos";

    public static readonly IReadOnlyList<string> Aceitos = new[]
    {
        Admin, Gerente, Vendedor, Caixa, Estoquista
    };

    public static bool EhValido(string? role) => role is not null && Aceitos.Contains(role);
}
```

`RolesUsuario` vai além de constantes: `Aceitos` alimenta o dropdown de perfil no cadastro de
usuário, e `EhValido()` revalida a role no POST. A lista existe em um lugar só — acrescentar
um perfil novo é acrescentar uma constante e a entrada em `Aceitos`, não caçar strings
espalhadas.

Os filtros de query string passam por um normalizador que rejeita valor desconhecido,
garantindo comportamento previsível quando a URL é editada à mão:

```csharp
private static string NormalizarSituacao(string? situacao) => situacao switch
{
    SituacaoFiltro.Ativos   => SituacaoFiltro.Ativos,
    SituacaoFiltro.Inativos => SituacaoFiltro.Inativos,
    _                       => SituacaoFiltro.Todos
};
```

### `PaginacaoViewModel` — paginação reutilizável

Um único tipo resolve a paginação de todas as listagens, com a aritmética encapsulada em
propriedades calculadas.

```csharp
public class PaginacaoViewModel
{
    public const int RegistrosPorPagina = 12;

    public int PaginaAtual    { get; private set; } = 1;
    public int TotalPaginas   { get; private set; } = 1;
    public int TotalRegistros { get; private set; }

    public int RegistrosParaPular => (PaginaAtual - 1) * RegistrosPorPagina;
    public int PrimeiroRegistro   => TotalRegistros == 0 ? 0 : RegistrosParaPular + 1;
    public int UltimoRegistro     => Math.Min(PaginaAtual * RegistrosPorPagina, TotalRegistros);

    public bool TemPaginaAnterior => PaginaAtual > 1;
    public bool TemProximaPagina  => PaginaAtual < TotalPaginas;
    public bool TemRegistros      => TotalRegistros > 0;

    public static PaginacaoViewModel Criar(int pagina, int totalRegistros)
    {
        var registros = Math.Max(totalRegistros, 0);

        var totalPaginas = registros == 0
            ? 1
            : (int)Math.Ceiling(registros / (double)RegistrosPorPagina);

        return new PaginacaoViewModel
        {
            TotalRegistros = registros,
            TotalPaginas   = totalPaginas,
            PaginaAtual    = Math.Clamp(pagina, 1, totalPaginas)
        };
    }
}
```

Decisões relevantes:

- **Setters `private`, construção por factory.** `Criar()` é o único caminho para instanciar,
  o que garante que os três campos base sempre estejam coerentes entre si. Não existe estado
  inválido possível.
- **`Math.Clamp(pagina, 1, totalPaginas)`.** A página vem da query string e pode ser
  qualquer coisa — `?pagina=0`, `?pagina=999`, `?pagina=-5`. O *clamp* normaliza sem lançar
  exceção nem exigir validação no controller.
- **Propriedades calculadas em vez de campos.** `RegistrosParaPular`, `PrimeiroRegistro` e
  `UltimoRegistro` derivam do estado; não há como ficarem dessincronizados.
- **Booleanos prontos para a View.** `TemProximaPagina` no Razor é mais legível — e menos
  sujeito a erro de comparação — do que `PaginaAtual < TotalPaginas` repetido em cada tela.

Os métodos de extensão aplicam a paginação tanto no banco quanto em memória:

```csharp
public static class PaginacaoExtensoes
{
    public static IQueryable<T> Pagina<T>(this IQueryable<T> consulta, PaginacaoViewModel paginacao) =>
        consulta
            .Skip(paginacao.RegistrosParaPular)
            .Take(PaginacaoViewModel.RegistrosPorPagina);

    public static IReadOnlyList<T> Pagina<T>(this IReadOnlyList<T> registros, PaginacaoViewModel paginacao) =>
        registros
            .Skip(paginacao.RegistrosParaPular)
            .Take(PaginacaoViewModel.RegistrosPorPagina)
            .ToList();
}
```

A sobrecarga sobre `IQueryable<T>` vira `OFFSET ... FETCH NEXT` no SQL — o banco devolve só
as 12 linhas da página. A sobrecarga sobre `IReadOnlyList<T>` atende os casos em que a
coleção já foi materializada, como relatórios que agregam em memória antes de exibir.

### Transações

Toda operação que toca estoque roda dentro de transação explícita. É a única forma de manter
`Tb_Produto.quantidade_atual` e `Tb_Movimentacao_Estoque` coerentes entre si.

**Registro de venda** (`VendasController.Create`):

```csharp
await using var transacao = await _context.Database.BeginTransactionAsync();

_context.Vendas.Add(venda);
await _context.SaveChangesAsync();          // 1º save: precisa do venda.Id gerado

foreach (var item in itensPreenchidos)
{
    var produto = produtos[item.ProdutoId];
    var quantidadeAntes  = produto.QuantidadeAtual;
    var quantidadeDepois = quantidadeAntes - item.Quantidade;

    _context.ItensVenda.Add(new ItemVenda
    {
        VendaId       = venda.Id,
        ProdutoId     = produto.Id,
        Quantidade    = item.Quantidade,
        PrecoUnitario = produto.PrecoVenda,   // snapshot
        PrecoCusto    = produto.PrecoCusto    // snapshot
    });

    _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
    {
        EmpresaId                 = empresaId,
        ProdutoId                 = produto.Id,
        UsuarioId                 = usuarioId!.Value,
        VendaId                   = venda.Id,          // vincula a baixa à venda
        TipoMovimentacaoEstoqueId = tipoSaida!.Id,
        Quantidade                = item.Quantidade,
        QuantidadeAntes           = quantidadeAntes,
        QuantidadeDepois          = quantidadeDepois,
        DataMovimentacao          = DateTime.Now,
        Observacao                = "Baixa automática pela venda."
    });

    produto.QuantidadeAtual = quantidadeDepois;
}

RegistrarLog("CRIACAO", venda.Id,
    $"Venda registrada: {itensPreenchidos.Count} item(ns), valor final {valorFinal:C}.");

await _context.SaveChangesAsync();
await transacao.CommitAsync();
```

O primeiro `SaveChangesAsync()` é necessário porque `venda.Id` é `IDENTITY` — só existe
depois do INSERT, e os itens e movimentações precisam dele como FK. Como tudo está sob a
mesma transação, uma falha posterior desfaz também esse INSERT.

O `await using` garante `RollbackAsync()` automático se qualquer linha entre o `Begin` e o
`Commit` lançar exceção.

**Bloqueio pessimista.** Dois usuários vendendo o mesmo produto ao mesmo tempo poderiam ler o
mesmo saldo e ambos gravarem uma baixa sobre ele. O `MovimentacoesController` previne isso
lendo o produto com *hint* de bloqueio:

```csharp
private Task<Produto?> BloquearProdutoAsync(int produtoId, int empresaId)
{
    const string sql = @"
        SELECT *
          FROM Tb_Produto WITH (UPDLOCK, ROWLOCK)
         WHERE id = {0} AND empresa_id = {1}";

    return _context.Produtos
        .FromSqlRaw(sql, produtoId, empresaId)
        .AsTracking()
        .FirstOrDefaultAsync();
}
```

`UPDLOCK` reserva a linha para atualização já na leitura; `ROWLOCK` restringe o bloqueio à
linha, evitando escalonamento para página ou tabela. A segunda transação concorrente aguarda
o commit da primeira e então lê o saldo já atualizado.

Os parâmetros são passados por *placeholders* `{0}` e `{1}` do `FromSqlRaw`, que o EF Core
converte em parâmetros do comando — não há concatenação de string, portanto não há superfície
para injeção de SQL.

**Cancelamento de venda** segue a mesma disciplina: marca
`situacao_venda = 'CANCELADA'`, localiza as movimentações originais por `venda_id`, gera
movimentações de **entrada** correspondentes e atualiza o saldo — tudo em uma transação. As
movimentações originais **não são apagadas**; o estorno é um lançamento novo, preservando a
rastreabilidade.

```csharp
var movimentacoesOriginais = await _context.MovimentacoesEstoque
    .AsNoTracking()
    .Where(m => m.VendaId == id && m.EmpresaId == empresaId)
    .ToListAsync();
```

**Padrão de snapshot histórico.** Valores que mudam com o tempo são copiados no momento da
transação, nunca lidos por referência depois:

| Coluna | Origem | Por quê |
|---|---|---|
| `preco_unitario` | `Tb_Produto.preco_venda` no momento da venda | Se o preço subir amanhã, a venda de ontem continua valendo o que valeu. |
| `preco_custo` | `Tb_Produto.preco_custo` no momento da venda | O lucro bruto de uma venda antiga precisa do custo daquela época. |

O mesmo raciocínio vale para `quantidade_antes` e `quantidade_depois`: o saldo é fotografado
antes e depois de cada operação, tornando o histórico auditável sem recomputar a cadeia
inteira.

### Exclusão lógica e exclusão física

O sistema usa **exclusão lógica** (`ativo = 0`) como regra. Nenhum cadastro é removido por
padrão — inativar preserva o histórico de vendas e movimentações que referenciam o registro.

A exclusão **física** existe como exceção, protegida por uma cadeia de verificações:

```csharp
// 1. Só permite excluir o que já está inativo
if (produto.Ativo)
{
    TempData["Erro"] = $"O produto {nome} precisa ser inativado antes de ser excluído.";
    return RedirectToAction(nameof(Index));
}

// 2. Verifica vínculo com itens de venda
var quantidadeItens = await _context.ItensVenda
    .AsNoTracking()
    .CountAsync(i => i.ProdutoId == id && i.Venda.EmpresaId == empresaId);

if (quantidadeItens > 0)
{
    TempData["Erro"] =
        $"O produto {nome} aparece em {quantidadeItens} item(ns) de venda e não pode ser excluído.";
    return RedirectToAction(nameof(Index));
}

// 3. Verifica vínculo com movimentações de estoque
var quantidadeMovimentacoes = await _context.MovimentacoesEstoque
    .AsNoTracking()
    .CountAsync(m => m.ProdutoId == id && m.EmpresaId == empresaId);
```

As mensagens informam a **quantidade** de vínculos, não apenas que a operação falhou — o
usuário entende o motivo sem precisar investigar. As listagens exibem esses contadores por
linha, carregados na consulta agregada descrita em [Anatomia de um CRUD](#anatomia-de-um-crud).

### Auditoria

Operações críticas geram registro em `Tb_Log_Sistema`, com vocabulário padronizado:

| Ação | Quando |
|---|---|
| `CRIACAO` | Inserção de registro |
| `ALTERACAO` | Edição sem mudança de status |
| `INATIVACAO` | Transição `ativo: true → false` |
| `REATIVACAO` | Transição `ativo: false → true` |
| `EXCLUSAO` | Remoção física |
| `CANCELAMENTO` | Cancelamento de venda |

O `RegistrosController` (restrito a `ADMIN`) expõe a consulta ao log com filtro por entidade,
ação, período e busca textual, com paginação.

Paralelamente, o `ILogger<T>` registra eventos técnicos com *structured logging* —
placeholders nomeados em vez de interpolação, o que mantém os campos consultáveis:

```csharp
_logger.LogInformation(
    "Venda {VendaId} criada na empresa {EmpresaId} com {QtdItens} item(ns).",
    venda.Id, empresaId, itensPreenchidos.Count);
```

Os dois mecanismos são complementares: `Tb_Log_Sistema` é auditoria de negócio, consultável
pelo administrador; `ILogger` é diagnóstico técnico, consumido pela infraestrutura.

### Mapeamento EF Core

As propriedades C# são PascalCase e as colunas são snake_case. Onde a diferença é apenas de
caixa (`Nome`/`nome`), a collation resolve. Onde há **underscore**, o mapeamento explícito é
obrigatório — sem `HasColumnName`, a query falha em runtime com *"Invalid column name"*:

```csharp
modelBuilder.Entity<Venda>(venda =>
{
    venda.Property(v => v.EmpresaId).HasColumnName("empresa_id");
    venda.Property(v => v.FuncionarioId).HasColumnName("funcionario_id");
    venda.Property(v => v.ClienteId).HasColumnName("cliente_id");
    venda.Property(v => v.FormaPagamentoId).HasColumnName("forma_pagamento_id");
    venda.Property(v => v.DataVenda).HasColumnName("data_venda");
    venda.Property(v => v.ValorTotal).HasColumnName("valor_total").HasPrecision(10, 2);
    venda.Property(v => v.ValorFinal).HasColumnName("valor_final").HasPrecision(10, 2);
    venda.Property(v => v.Desconto).HasPrecision(10, 2);
    venda.Property(v => v.SituacaoVenda).HasColumnName("situacao_venda");
});
```

Regras que valem para toda entidade:

| Regra | Motivo |
|---|---|
| `ToTable("Tb_...")` explícito | O EF pluralizaria pelo nome da classe. As 15 tabelas são declaradas. |
| `HasColumnName` em toda coluna com underscore | Sem isso, *"Invalid column name"* em runtime. |
| `HasPrecision(10, 2)` em todo `decimal` | Sem isso o EF avisa *"values silently truncated"* — inaceitável em valores monetários. |
| Toda entidade precisa de PK descoberta pelo EF | A validação do modelo é **global** e roda no primeiro uso do `DbContext`: uma entidade sem chave derruba *qualquer* operação, inclusive o login. |
| `.ValueGeneratedOnAddOrUpdate()` em coluna calculada | `Tb_Item_Venda.subtotal` é gerada pelo banco; tentar gravá-la falha. |
| `.HasColumnType("date")` em `DATE` | `data_admissao` e `data_despesa` não têm componente de hora. |
| `.IsRequired(false)` em FK nullable | `MovimentacaoEstoque.VendaId` é opcional — ajuste manual não tem venda associada. |

### Camada de apresentação

**Partial `_Formulario.cshtml`.** Cada módulo tem uma única partial de formulário, consumida
por `Create`, `Edit` e `Details`. O modo somente leitura é controlado pela flag
`SomenteLeitura` do ViewModel, evitando três arquivos com marcação quase idêntica.

**Tag Helpers.** Os formulários usam `asp-for`, `asp-action`, `asp-items` e
`asp-validation-for`. O `asp-for` conecta o campo ao ViewModel gerando `name`, `id`, rótulo
(via `[Display]`) e os atributos `data-val-*` que a validação client-side consome.

**Cultura pt-BR.** Configurada no `Program.cs` via `UseRequestLocalization`, o que faz
`decimal` aceitar vírgula como separador decimal e datas seguirem o formato brasileiro no
*model binding* e na renderização.

**Módulos JavaScript.** Cada arquivo em `wwwroot/js/` é uma IIFE que verifica a existência do
seu elemento-âncora e retorna cedo se não o encontrar. Isso permite carregar os scripts no
layout sem que um deles quebre em páginas onde não se aplica.

| Arquivo | Responsabilidade |
|---|---|
| `venda-itens.js` | Linhas dinâmicas de item de venda, com reindexação dos `name` para o model binding de coleções e cálculo do total em tempo real |
| `movimentacao-estoque.js` | Preenche "quantidade depois" conforme a natureza do tipo selecionado |
| `mascara-cpf.js` | Máscara de CPF na exibição |
| `validacao-ptbr.js` | Mensagens de validação em português |
| `modal.js` | Confirmação de ações destrutivas via `<dialog>` nativo |
| `dashboard.js` | Renderização dos gráficos Chart.js a partir de JSON embutido na página |

**Dependências vendorizadas.** Bootstrap, jQuery, jQuery Validation e Chart.js estão
versionados em `wwwroot/lib/`, sem CDN. Garante que a aplicação funcione sem internet.

**Pipeline de requisição** (`Program.cs`), na ordem de registro:

```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();   // identifica quem é
app.UseAuthorization();    // decide o que pode
app.MapControllerRoute(...);
```

`UseAuthentication` precede `UseAuthorization` — não há como autorizar antes de saber quem é
o usuário.

**Autorização global.** O filtro registrado no `AddControllersWithViews` exige usuário
autenticado em **toda** action:

```csharp
var politica = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

options.Filters.Add(new AuthorizeFilter(politica));
```

O padrão é *deny by default*: o que for público precisa de `[AllowAnonymous]` explícito
(apenas o `AccountController`). Esquecer de proteger um controller novo não abre brecha — ele
já nasce protegido.

**Configuração do cookie:**

```csharp
options.Cookie.Name       = "LOSolutions.Auth";
options.Cookie.HttpOnly   = true;                  // inacessível a JavaScript — mitiga XSS
options.Cookie.SameSite   = SameSiteMode.Lax;      // mitiga CSRF
options.ExpireTimeSpan    = TimeSpan.FromHours(8); // jornada de trabalho
options.SlidingExpiration = true;                  // renova enquanto houver atividade
```

---

## Serviços de aplicação

A pasta `Services/` guarda integrações externas e utilitários transversais — o que não
pertence a nenhum controller específico e não é regra de negócio.

### `PrevisaoService` — HttpClient tipado

Encapsula a comunicação com a API analítica em Python. Registrado como **typed client** no
`Program.cs`, o que traz gerenciamento de `HttpClientHandler` pelo `IHttpClientFactory` —
evitando tanto o esgotamento de sockets quanto o problema de DNS obsoleto de um `HttpClient`
estático.

```csharp
builder.Services.AddHttpClient<PrevisaoService>(cliente =>
{
    cliente.BaseAddress = new Uri(
        builder.Configuration["Analytics:UrlBase"] ?? "http://localhost:8000");

    cliente.Timeout = TimeSpan.FromSeconds(10);
});
```

A URL vem da configuração, com *fallback* para o endereço local de desenvolvimento. O timeout
de 10 segundos impede que a tela de previsão fique pendurada se o serviço travar.

```csharp
public async Task<RespostaPrevisaoDto?> PreverAsync(
    RequisicaoPrevisaoDto requisicao, CancellationToken cancelamento = default)
{
    try
    {
        var resposta = await _http.PostAsJsonAsync("previsao", requisicao, cancelamento);

        if (!resposta.IsSuccessStatusCode)
        {
            _logger.LogWarning("Serviço de previsão respondeu {StatusCode}.",
                (int)resposta.StatusCode);
            return null;
        }

        return await resposta.Content.ReadFromJsonAsync<RespostaPrevisaoDto>(cancelamento);
    }
    catch (Exception excecao) when (
        excecao is HttpRequestException || excecao is TaskCanceledException)
    {
        _logger.LogError(excecao,
            "Não foi possível contatar o serviço de previsão {BaseAddress}. " +
            "Verifique se o uvicorn está rodando.", _http.BaseAddress);

        return null;
    }
}
```

Decisões relevantes:

- **Retorna `null` em vez de lançar.** Serviço indisponível não é erro da aplicação — é um
  cenário previsto. O controller trata o `null` exibindo uma mensagem na tela, e o resto do
  sistema continua funcionando normalmente.
- **`catch` filtrado com `when`.** Captura apenas `HttpRequestException` (rede) e
  `TaskCanceledException` (timeout). Um bug de desserialização continua propagando, em vez de
  ser mascarado como "serviço fora do ar".
- **`CancellationToken` propagado.** Se o usuário abandona a página, a requisição HTTP é
  cancelada em vez de segurar a thread até o timeout.
- **A mensagem de log diz o que fazer.** Citar o `BaseAddress` e lembrar do uvicorn poupa
  tempo de diagnóstico.

**Contrato JSON.** Os DTOs em `PrevisaoContratos.cs` mapeiam PascalCase do C# para snake_case
do Python via `[JsonPropertyName]`:

```csharp
public class RequisicaoPrevisaoDto
{
    [JsonPropertyName("meses_previsao")]
    public int MesesPrevisao { get; set; }

    [JsonPropertyName("series")]
    public List<SerieHistoricaDto> Series { get; set; } = new();
}
```

Cada classe C# tem contraparte direta em um modelo Pydantic do lado Python. O atributo
explícito, em vez de uma política global de serialização, deixa o contrato legível no próprio
código e imune a mudança de configuração.

### `PlanilhaRelatorio` — exportação para Excel

Classe estática que converte um `RelatorioViewModel` em `.xlsx` usando **ClosedXML**. O
mesmo ViewModel alimenta a tela e o arquivo, então não há risco de a planilha divergir do que
o usuário viu.

```csharp
public const string TipoConteudo =
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

public static byte[] Gerar(RelatorioViewModel relatorio)
{
    using var pasta = new XLWorkbook();
    var planilha = pasta.Worksheets.Add(NomeDaAba(relatorio.Tipo));
    var ultimaColuna = Math.Max(relatorio.Colunas.Count, 1);

    var linhaCabecalho = EscreverCabecalhoDoDocumento(planilha, relatorio, ultimaColuna);
    EscreverColunas(planilha, relatorio, linhaCabecalho);

    var proximaLinha = EscreverLinhas(planilha, relatorio, linhaCabecalho);
    EscreverTotais(planilha, relatorio, proximaLinha, ultimaColuna);

    planilha.SheetView.FreezeRows(linhaCabecalho);
    planilha.Columns().AdjustToContents(linhaCabecalho, linhaCabecalho + relatorio.Linhas.Count);

    using var memoria = new MemoryStream();
    pasta.SaveAs(memoria);
    return memoria.ToArray();
}
```

O arquivo traz cabeçalho com empresa, período, filtros aplicados e data de emissão — o que
torna a planilha autoexplicativa depois de baixada. `FreezeRows` mantém o cabeçalho visível
na rolagem e `AdjustToContents` dimensiona as colunas pelo conteúdo.

A entrega usa o `FileResult` do ASP.NET Core:

```csharp
var planilha = PlanilhaRelatorio.Gerar(relatorio);
return File(planilha, PlanilhaRelatorio.TipoConteudo, relatorio.NomeArquivo);
```

O nome do arquivo é derivado do próprio relatório
(`relatorio-vendas-20260101-a-20260131.xlsx`), o que evita colisão na pasta de downloads
quando o usuário exporta vários períodos.

Três tipos de relatório compartilham a mesma estrutura genérica de colunas, linhas e totais:
`movimentacoes`, `vendas` e `despesas`.

### `Aparencia` — preferências de interface

Tema, tamanho de fonte e densidade são preferências de **apresentação**, não dados de
negócio. Ficam em cookie, não no banco.

```csharp
public class PreferenciaAparencia
{
    private readonly string[] _valores;

    public PreferenciaAparencia(string cookie, params string[] valores)
    {
        Cookie = cookie;
        _valores = valores;
    }

    public string Cookie { get; }
    public string Padrao => _valores[0];

    public string Normalizar(string? valor) =>
        valor is not null && _valores.Contains(valor) ? valor : Padrao;

    public string Atual(HttpRequest requisicao) => Normalizar(requisicao.Cookies[Cookie]);

    public void Gravar(HttpResponse resposta, string? valor) =>
        resposta.Cookies.Append(Cookie, Normalizar(valor), new CookieOptions
        {
            Expires     = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly    = true,
            IsEssential = true,
            SameSite    = SameSiteMode.Lax
        });
}

public static class Aparencia
{
    public static readonly PreferenciaAparencia Tema =
        new("LOSolutions.Tema", TemaEscuro, TemaClaro, TemaDispositivo);

    public static readonly PreferenciaAparencia Fonte =
        new("LOSolutions.Fonte", FonteNormal, FonteGrande, FonteMaior);

    public static readonly PreferenciaAparencia Densidade =
        new("LOSolutions.Densidade", DensidadeConfortavel, DensidadeCompacta);
}
```

O ponto do design é que **`Normalizar()` é aplicado na leitura e na escrita**. Um cookie
adulterado ou corrompido não chega à View: o valor cai para o padrão silenciosamente. O
primeiro item do `params` é sempre o padrão, então acrescentar uma opção nova é acrescentar
um argumento no construtor.

As opções de fonte e densidade também atendem ao requisito de interface acessível a usuários
com diferentes níveis de familiaridade tecnológica.

### `ExpiracaoLogBackgroundService` — retenção de log

`BackgroundService` que remove registros de auditoria com mais de 12 meses, executando a cada
24 horas.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var dataLimite = DateTime.Now.AddMonths(-12);

            var linhasRemovidas = await context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM Tb_Log_Sistema WHERE data_Hora < {dataLimite}", stoppingToken);

            _logger.LogInformation(
                "Expiração de log após 12 meses. Linhas removidas: {Linhas}", linhasRemovidas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao executar expiração de logs.");
        }

        await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
    }
}
```

Quatro detalhes que importam:

- **`IServiceScopeFactory` em vez de injetar o `AppDbContext`.** O `DbContext` é registrado
  com tempo de vida *scoped*, e um `BackgroundService` é *singleton*. Injetá-lo diretamente
  manteria o mesmo contexto vivo durante toda a execução da aplicação, acumulando entidades
  rastreadas. Criar um escopo por ciclo resolve.
- **`ExecuteSqlInterpolatedAsync` em vez de carregar e remover.** Um `DELETE` direto no banco
  evita materializar milhares de entidades só para descartá-las. A interpolação é convertida
  em parâmetro pelo EF Core — não é concatenação de string.
- **`try/catch` dentro do laço.** Uma falha em um ciclo — banco indisponível, por exemplo —
  registra o erro e aguarda o próximo. Sem isso, a exceção encerraria o serviço em silêncio
  e a limpeza nunca mais rodaria.
- **`stoppingToken` propagado no `Delay` e na query.** No desligamento da aplicação o serviço
  encerra imediatamente, em vez de segurar o processo por até 24 horas.

Essa é a contrapartida à regra de que log nunca é apagado manualmente: a remoção é
automática, uniforme e registrada.

---

## Camada analítica

Serviço HTTP independente, em Python, que recebe séries mensais e devolve projeções por
regressão linear. Fica em `analytics/`.

### Contrato

```
GET  /saude      → diagnóstico: { "status": "ok", "servico": "previsao" }
POST /previsao   → recebe séries históricas, devolve séries projetadas
```

Os modelos são declarados com **Pydantic**, o que dá validação automática do payload antes de
o código de negócio rodar:

```python
class PontoHistorico(BaseModel):
    """Um mês fechado do histórico. Espelha PontoSerieDto.cs no C#"""
    ano: int
    mes: int = Field(ge=1, le=12)
    valor: float

class RequisicaoPrevisao(BaseModel):
    meses_previsao: int = Field(ge=1, le=24)
    series: List[SerieHistorica]
```

`Field(ge=..., le=...)` rejeita mês fora de 1–12 e horizonte fora de 1–24 com resposta HTTP
422, sem chegar ao modelo. O limite superior de 24 meses é deliberado: extrapolação linear
muito longa produz número sem significado.

### Modelo de previsão

Regressão linear simples (`scikit-learn`) sobre o índice do mês:

```python
MINIMO_PONTOS = 3

def prever_serie(serie: SerieHistorica, meses_previsao: int) -> SeriePrevista:
    if len(serie.pontos) < MINIMO_PONTOS:
        return SeriePrevista(nome=serie.nome, suficiente=False, ...)

    df = pd.DataFrame([ponto.model_dump() for ponto in serie.pontos])
    df = df.sort_values(["ano", "mes"]).reset_index(drop=True)
    df["indice_mes"] = range(len(df))

    x = df[["indice_mes"]].to_numpy().reshape(-1, 1)
    y = df["valor"].to_numpy()

    modelo = LinearRegression()
    modelo.fit(x, y)

    r2 = float(r2_score(y, modelo.predict(x)))
    ...
```

Decisões do modelo:

| Decisão | Justificativa |
|---|---|
| **Mínimo de 3 pontos** | Com dois pontos a reta passa exatamente por ambos e o R² é sempre 1 — número sem informação. Abaixo do mínimo, a série volta com `suficiente: false` e a tela informa em vez de exibir projeção falsa. |
| **`suficiente` no contrato de resposta** | Histórico insuficiente não é erro HTTP: é um resultado legítimo. Sinalizar no corpo permite tratar série a série — faturamento pode ter dados e despesas não. |
| **R² retornado junto** | O usuário vê o quanto a reta explica o histórico. Uma projeção com R² baixo é apresentada com ressalva, não como certeza. |
| **Índice sequencial em vez de data** | `indice_mes` é `0, 1, 2, ...`. A regressão trabalha sobre a posição no histórico, não sobre timestamp — dispensa `datetime` e evita distorção por meses de tamanhos diferentes. |
| **Ordenação defensiva** | `sort_values(["ano", "mes"])` mesmo com o C# já enviando ordenado. Se a ordem mudar na origem, o modelo não passa a treinar sobre série embaralhada. |
| **Piso em zero** | `max(0.0, float(valor))` — faturamento e despesa projetados negativos não têm sentido de negócio. Uma tendência de queda acentuada seria extrapolada para valores negativos sem esse corte. |
| **Aritmética de calendário própria** | `avancar_mes()` opera em meses absolutos (`ano * 12 + mes - 1`), sem `datetime`. O dia é irrelevante para série mensal. |

A resposta inclui o coeficiente angular e o intercepto, o que permite descrever a tendência
em termos de negócio ("crescimento médio de R$ X por mês") em vez de apenas plotar a linha.

### Integração com a aplicação

O `PrevisaoController` monta as séries a partir do banco, respeitando o isolamento por
empresa e o filtro de vendas concluídas:

```csharp
var faturamentoPorMes = await _context.Vendas
    .AsNoTracking()
    .Where(v => v.EmpresaId == empresaId
             && v.SituacaoVenda == SituacaoVenda.Concluida
             && v.DataVenda >= inicio
             && v.DataVenda < fimExclusivo)
    .GroupBy(v => new { v.DataVenda.Year, v.DataVenda.Month })
    .Select(grupo => new { grupo.Key.Year, grupo.Key.Month, Total = grupo.Sum(v => v.ValorFinal) })
    .ToListAsync();
```

O horizonte é limitado no controller antes de chegar à API:

```csharp
var horizonte = Math.Clamp(mesesPrevisao ?? MesesPrevisaoPadrao, 1, MesesPrevisaoMaximo);
var fimExclusivo = fim.AddDays(1);   // necessário porque data_venda é DATETIME
```

O `fimExclusivo` merece nota: como `data_venda` é `DATETIME`, comparar `<= fim` excluiria as
vendas do próprio dia final a partir das 00:00:01. Usar `< fim.AddDays(1)` captura o dia
inteiro.

Padrões: 24 meses de histórico, 6 meses de projeção.

### Executando o serviço

```bash
cd analytics
python -m venv .venv
.venv\Scripts\activate          # Windows
pip install -r requirements.txt
uvicorn previsao_api:app --reload --port 8000
```

Documentação interativa gerada automaticamente pelo FastAPI em `http://localhost:8000/docs`.

> A aplicação .NET funciona normalmente com o serviço fora do ar — apenas a tela de previsão
> exibe aviso de indisponibilidade. Nenhum outro módulo depende dele.

---

## Requisitos e regras de negócio

A elicitação produziu 36 requisitos funcionais, 9 requisitos não funcionais e 15 regras
de negócio. O artigo apresenta uma seleção; a relação integral está aqui, com o ponto de
implementação de cada item.

Status: **OK** implementado · **Parcial** implementado com ressalva · **Pendente** previsto
como evolução.

### Requisitos funcionais

#### Cadastros e configurações

| # | Requisito | Onde | Status |
|---|---|---|---|
| 1 | CRUD de usuários com perfil e status | `UsuariosController` | OK |
| 2 | Autenticação com validação de perfil | `AccountController` + `AuthorizeFilter` global | OK |
| 3 | CRUD de empresas | `ConfiguracoesController` | Parcial — `Tb_Empresa` tem `nome`, não razão social e nome fantasia separados |
| 4 | CRUD de funcionários | `FuncionariosController` | Parcial — sem campo de e-mail |
| 5 | CRUD de clientes | `ClientesController` | Parcial — só CPF; CNPJ de cliente não suportado |
| 6 | CRUD de categorias de produto | `CategoriasController` | OK |
| 7 | CRUD de produtos | `ProdutosController` | Parcial — sem unidade de medida |
| 8 | CRUD de formas de pagamento | `FormasPagamentoController` | OK |
| 9 | CRUD de categorias de despesa | `CategoriasDespesaController` | Parcial — sem classificação |
| 10 | Configurações gerais | `ConfiguracoesController` + `Aparencia` | OK |

#### Estoque, vendas e financeiro

| # | Requisito | Onde | Status |
|---|---|---|---|
| 11 | Registro de movimentações de estoque | `MovimentacoesController` | OK |
| 12 | Tipos de movimentação | `Tb_Tipo_Movimentacao` | OK — o ajuste manual é modelado como dois tipos (`Ajuste de entrada` / `Ajuste de saída`), pois `natureza` é fixa por tipo |
| 13 | Atualização automática do saldo | `MovimentacoesController`, dentro de transação | OK |
| 14 | Registro de vendas | `VendasController.Create` | OK |
| 15 | Itens de venda com produto, quantidade, valor unitário e subtotal | `Tb_Item_Venda`, `subtotal` como coluna calculada | OK — desconto aplicado sobre o total da venda, não por item |
| 16 | Cálculo automático do total com desconto | `VendasController` + `CHK_Venda_ValorFinal` | OK |
| 17 | Baixa automática de estoque na confirmação | `VendasController.Create`, mesma transação | OK |
| 18 | Cancelamento com reversão de estoque | `VendasController.Cancelar` (perfil `ADMIN`) | OK |
| 19 | Lançamento de despesas | `DespesasController` | Parcial — registra natureza (fixa/eventual), não forma de pagamento |
| 20 | Consulta de vendas com filtros | `VendasController.Index` | OK |
| 21 | Consulta de movimentações com filtros | `MovimentacoesController.Index` | OK |
| 22 | Consulta de despesas com filtros | `DespesasController.Index` | OK |
| 23 | Alerta de estoque mínimo | `ProdutoListaViewModel.EstoqueBaixo` e `Vw_Produtos_Abaixo_Estoque_Minimo` (critério `<=`) | OK |
| 24 | Lucro bruto estimado | `DashboardController`, com snapshot de custo em `Tb_Item_Venda` | OK |

#### Relatórios, indicadores e analítico

| # | Requisito | Onde | Status |
|---|---|---|---|
| 25 | Relatório de produtos | `RelatoriosController` | OK |
| 26 | Relatório de clientes com histórico de compras | `RelatoriosController` | OK |
| 27 | Relatório de funcionários | `RelatoriosController` | OK |
| 28 | Relatório de vendas por período | `RelatoriosController` + `PlanilhaRelatorio` | OK |
| 29 | Relatório de despesas por período | `RelatoriosController` + `PlanilhaRelatorio` | OK |
| 30 | Relatório de movimentações | `RelatoriosController` + `PlanilhaRelatorio` | OK |
| 31 | Indicadores gerenciais | `DashboardController` | OK |
| 32 | Dashboards gerenciais | `DashboardController` + Chart.js na própria aplicação | OK — sem Power BI |
| 33 | Integração com rotinas Python | `PrevisaoService` → API FastAPI | OK |
| 34 | Análises preditivas | `analytics/previsao_api.py`, regressão linear | Parcial — faturamento e despesas; reposição de estoque pendente |
| 35 | Assistente em linguagem natural | — | Pendente |
| 36 | Log de auditoria | `ControllerValidacao.RegistrarLog()` + `RegistrosController` | OK |

### Requisitos não funcionais

| # | Requisito | Onde |
|---|---|---|
| 37 | Autenticação obrigatória | `AuthorizeFilter` global — *deny by default* |
| 38 | Controle de acesso por perfil | `[Authorize(Roles = ...)]` por controller |
| 39 | Isolamento de dados por empresa | `EmpresaIdAtual()` em toda consulta + `empresa_id NOT NULL` em 13 das 15 tabelas |
| 40 | Integridade dos dados | FKs, constraints `CHECK`, índices únicos, colunas calculadas |
| 41 | Desempenho compatível com uso cotidiano | 13 índices no padrão `(empresa_id, filtro)` com `INCLUDE`; `AsNoTracking()` em leituras; paginação |
| 42 | Interface clara e organizada | Layout único, formulário compartilhado, preferências de tema, fonte e densidade |
| 43 | ASP.NET MVC + SQL Server | .NET 8, EF Core 8, SQL Server 2019+ |
| 44 | Extensibilidade sem reestruturação | Camada analítica desacoplada por HTTP; controller base comum |
| 45 | Execução em navegadores atualizados | Bootstrap 5, dependências vendorizadas em `wwwroot/lib` |

### Regras de negócio

Implementadas como validação **no servidor**, independentemente da validação no cliente.

| # | Regra | Onde é garantida |
|---|---|---|
| 46 | Toda venda deve ter ao menos um item | Validação no `VendasController` antes do commit |
| 47 | Quantidade vendida não pode exceder o estoque | Validação por item no POST + `CHK_Produto_QtdAtual` no banco |
| 48 | Toda movimentação deve ter produto e tipo | FKs `NOT NULL` + validação na aplicação |
| 49 | Toda despesa deve ter categoria | FK `categoria_despesa_id NOT NULL` |
| 50 | Toda venda deve ter empresa e funcionário responsável | FKs `empresa_id` e `funcionario_id` `NOT NULL`; o usuário que registrou fica no log |
| 51 | Produto inativo não pode ser vendido | Dropdown filtrado + revalidação de `produto.Ativo` no POST |
| 52 | Operações críticas geram log | `RegistrarLog()` no mesmo `SaveChanges()` da operação |
| 53 | Movimentações não podem ser excluídas; correção por lançamento contrário | `MovimentacoesController` não expõe exclusão |
| 54 | Um produto não pode repetir em duas linhas da mesma venda | Validação por `GroupBy` no POST |
| 55 | Desconto não pode ser negativo nem exceder o total | Validação no POST + `CHK_Venda_Desconto` e `CHK_Venda_ValorFinal` |
| 56 | Preço de venda não pode ser inferior ao preço de custo | `CHK_Produto_Preco` |
| 57 | Cadastros são inativados, não excluídos | Exclusão física só para registro inativo e sem vínculos — ver [Exclusão lógica e exclusão física](#exclusão-lógica-e-exclusão-física) |
| 58 | Venda cancelada não compõe apuração financeira | Filtro `situacao_venda = 'CONCLUIDA'` no dashboard, relatórios e previsão |
| 59 | Preços são congelados no momento da venda | `preco_unitario` e `preco_custo` em `Tb_Item_Venda` |
| 60 | Log de auditoria retido por 12 meses | `ExpiracaoLogBackgroundService` |

> A regra 57 vale para **registros de cadastro**. Tabelas de evento — venda, item de venda,
> movimentação, despesa e log — não possuem coluna `ativo`: a venda é cancelada, a
> movimentação é imutável e o log tem expurgo automático.

---

## Perfis de acesso

Armazenados em `Tb_Usuario.role` e restritos por `CHK_Usuario_Role`. O valor é lido no login
e gravado como `ClaimTypes.Role`, consumido por `[Authorize(Roles = "...")]`.

| Role | Escopo |
|---|---|
| `ADMIN` | Nível máximo — usuários, cancelamento de vendas, log de auditoria, dados da empresa |
| `GERENTE` | Cadastros, relatórios, dashboards, previsão e indicadores |
| `VENDEDOR` | Registro de vendas |
| `CAIXA` | Registro de vendas |
| `ESTOQUISTA` | Movimentação de estoque |

Autorização aplicada por controller:

| Controller | Roles autorizadas |
|---|---|
| `ProdutosController`, `ClientesController`, `FuncionariosController`, `CargosController`, `DespesasController`, `CategoriasController`, `CategoriasDespesaController`, `FormasPagamentoController`, `TiposMovimentacaoController`, `DashboardController`, `RelatoriosController`, `PrevisaoController` | `ADMIN`, `GERENTE` |
| `MovimentacoesController` | `ADMIN`, `GERENTE`, `ESTOQUISTA` |
| `VendasController` | `ADMIN`, `GERENTE`, `VENDEDOR`, `CAIXA` |
| `VendasController.Cancelar` | `ADMIN` — restrição adicional no nível da action |
| `UsuariosController`, `RegistrosController` | `ADMIN` |
| `ConfiguracoesController` | Qualquer usuário autenticado; a seção de dados da empresa só aparece para `ADMIN` |

> As roles são escritas em maiúsculas, exatamente como definidas no `CHECK` do banco — a
> claim vem direto da coluna, sem tradução intermediária.

---

## Como executar

### Pré-requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2019 ou superior (Express serve)
- Python 3.11+ (apenas para a camada analítica)
- Visual Studio 2022/2026 ou VS Code com o C# Dev Kit

### 1. Clonar

```bash
git clone https://github.com/otaviosalmon/TCC_SistemaEmpresa.git
cd TCC_SistemaEmpresa
```

### 2. Criar o banco

Execute os dois scripts na ordem, conectado à instância do SQL Server:

```
database/L.O_database.sql -- cria o banco, 15 tabelas, views e índices
database/seed_db.sql -- 2 empresas com 24 meses de vendas, despesas e movimentações
```

No Visual Studio: **View → SQL Server Object Explorer**, botão direito no servidor →
**New Query**, cole o script e execute com `Ctrl+Shift+E`.

O seed pode ser executado mais de uma vez: ele remove os dados das duas empresas antes de
inseri-los novamente. A execução leva de 30 segundos a 1 minuto.

**Usuários de acesso** (senha `Senha@123` para todos):

| Empresa | Usuários |
|---|---|
| Mercado Bom Preco LTDA | `admin.bp`, `gerente.bp`, `vendedor.bp`, `caixa.bp`, `estoquista.bp` |
| Tech Store Franca ME | `admin.ts`, `gerente.ts`, `vendedor.ts`, `caixa.ts`, `estoquista.ts` |

### 3. Configurar a connection string

A string de conexão **não é versionada**. Configure via *user secrets*:

```bash
cd TCC_SistemaEmpresa
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost\SQLEXPRESS;Database=SistemaGestaoComercial;Trusted_Connection=True;TrustServerCertificate=True;"
```

Se a API analítica rodar em outra porta ou host:

```bash
dotnet user-secrets set "Analytics:UrlBase" "http://localhost:8000"
```

### 4. Subir a camada analítica

```bash
cd analytics
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
uvicorn previsao_api:app --reload --port 8000
```

Opcional — só a tela de previsão depende dela.

### 5. Executar a aplicação

```bash
dotnet run --project TCC_SistemaEmpresa
```

A aplicação sobe em `https://localhost:7xxx`. A rota raiz exige autenticação e redireciona
para `/Account/Login`.

---

## Autores

**Lucas Macedo Abrahão** · **Otávio Salomão**
Ciência da Computação — Centro Universitário Municipal de Franca (Uni-FACEF)
Orientadora: Prof.ª Dra. Silvia Regina Viel

---

*Projeto acadêmico. O código está disponível para consulta e estudo.*
