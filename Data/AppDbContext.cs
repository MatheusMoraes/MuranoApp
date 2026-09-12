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

            // Índice único sobre o nome normalizado — checagem de duplicidade
            // de Product/Category vira uma busca indexada em vez de carregar
            // a tabela inteira pra memória a cada criação/edição.
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.NomeNormalizado)
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
        }
    }
}
