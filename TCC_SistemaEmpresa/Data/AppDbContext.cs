using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Models;
namespace TCC_SistemaEmpresa.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Empresa> Empresa { get; set; }
        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Cargo> Cargo { get; set; }
        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<CategoriaProduto> CategoriaProdutos { get; set; }
        public DbSet<CategoriaDespesa> CategoriasDespesa { get; set; }
        public DbSet<FormaPagamento> FormasPagamento { get; set; }
        public DbSet<Venda> Vendas { get; set; }
        public DbSet<MovimentacaoEstoque> MovimentacoesEstoque { get; set; }
        public DbSet<ItemVenda> ItensVenda { get; set; }
        public DbSet<Despesa> Despesas { get; set; }
        public DbSet<TipoMovimentacao> TiposMovimentacao { get; set; }
        public DbSet<LogSistema> LogsSistema { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Empresa>().ToTable("Tb_Empresa");
            modelBuilder.Entity<Usuario>().ToTable("Tb_Usuario");
            modelBuilder.Entity<Cargo>().ToTable("Tb_Cargo");
            modelBuilder.Entity<Funcionario>().ToTable("Tb_Funcionario");
            modelBuilder.Entity<Cliente>().ToTable("Tb_Cliente");
            modelBuilder.Entity<Produto>().ToTable("Tb_Produto");
            modelBuilder.Entity<CategoriaProduto>().ToTable("Tb_Categoria_Produto");
            modelBuilder.Entity<CategoriaDespesa>().ToTable("Tb_Categoria_Despesa");
            modelBuilder.Entity<FormaPagamento>().ToTable("Tb_Forma_Pagamento");
            modelBuilder.Entity<Venda>().ToTable("Tb_Venda");
            modelBuilder.Entity<MovimentacaoEstoque>().ToTable("Tb_Movimentacao_Estoque");
            modelBuilder.Entity<ItemVenda>().ToTable("Tb_Item_Venda");
            modelBuilder.Entity<Despesa>().ToTable("Tb_Despesa");
            modelBuilder.Entity<TipoMovimentacao>().ToTable("Tb_Tipo_Movimentacao");
            modelBuilder.Entity<LogSistema>().ToTable("Tb_Log_Sistema");


            modelBuilder.Entity<ItemVenda>()
                .Property(i => i.Subtotal)
                .ValueGeneratedOnAddOrUpdate();

            // As colunas do banco são snake_case (empresa_id, password_hash...) e as
            // propriedades são PascalCase. Onde os nomes só diferem em caixa o SQL Server
            // resolve sozinho (collation CI); onde há underscore, o mapeamento é obrigatório.
            // Mapeado aqui apenas o que o login consome — as demais entidades ainda precisam
            // do mesmo tratamento quando forem usadas.
            modelBuilder.Entity<Usuario>(usuario =>
            {
                usuario.Property(u => u.EmpresaId).HasColumnName("empresa_id");
                usuario.Property(u => u.PasswordHash).HasColumnName("password_hash");
                usuario.Property(u => u.DataCadastro).HasColumnName("data_cadastro");
            });

            modelBuilder.Entity<Cargo>(cargo =>
            {
                cargo.Property(c => c.EmpresaId).HasColumnName("empresa_id");
                cargo.Property(c => c.SalarioBase).HasColumnName("salario_base").HasPrecision(10, 2);
                cargo.Property(c => c.PerComissaoBase).HasColumnName("per_comissao_base").HasPrecision(5, 2);
            });

            modelBuilder.Entity<Funcionario>(funcionario =>
            {
                funcionario.Property(f => f.EmpresaId).HasColumnName("empresa_id");
                funcionario.Property(f => f.UsuarioId).HasColumnName("usuario_id");
                funcionario.Property(f => f.CargoId).HasColumnName("cargo_id");
                funcionario.Property(f => f.Salario).HasPrecision(10, 2);
                funcionario.Property(f => f.PerComissao).HasColumnName("per_comissao").HasPrecision(5, 2);
                // data_admissao é DATE no banco, não DATETIME.
                funcionario.Property(f => f.DataAdmissao).HasColumnName("data_admissao").HasColumnType("date");
            });

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
            });

            modelBuilder.Entity<LogSistema>(log =>
            {
                log.Property(l => l.EmpresaId).HasColumnName("empresa_id");
                log.Property(l => l.UsuarioId).HasColumnName("usuario_id");
                log.Property(l => l.EntidadeAfetada).HasColumnName("entidade_afetada");
                log.Property(l => l.RegistroId).HasColumnName("registro_id");
                log.Property(l => l.DataHora).HasColumnName("data_hora");
            });
        }
        
    }
}
