using Microsoft.EntityFrameworkCore;
using MuranoApp.Models;

namespace MuranoApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .Property(p => p.PrecoVarejo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.PrecoAtacado)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.ValorTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.PrecoUnitario)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Client)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdminUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice único composto (dono + nome normalizado) — nome de
            // produto só precisa ser único dentro do catálogo do mesmo
            // usuário (dois usuários podem ter produtos de mesmo nome em
            // catálogos separados). Checagem de duplicidade vira uma busca
            // indexada em vez de carregar a tabela inteira pra memória a
            // cada criação/edição.
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.UserId, p.NomeNormalizado })
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.NomeNormalizado)
                .IsUnique();

            // Produto pode ser excluído mesmo com pedidos associados — o
            // item do pedido só perde a referência viva (fica com
            // ProdutoId nulo) e continua existindo com o NomeProduto
            // salvo no momento da compra.
            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Produto)
                .WithMany()
                .HasForeignKey(i => i.ProdutoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Dono do cadastro (quem criou) — Restrict pra nunca apagar
            // produtos/pedidos de ninguém só por remover um usuário (hoje
            // não existe nem rota pra excluir usuário, mas evita a
            // armadilha por padrão).
            modelBuilder.Entity<Product>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
