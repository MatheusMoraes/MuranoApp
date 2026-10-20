using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MuranoApp.Data;
using MuranoApp.Models;

namespace MuranoApp.Tests
{
    // OrderService usa BeginTransactionAsync/CreateExecutionStrategy, APIs
    // relacionais que o provider InMemory do EF Core não suporta (lança
    // InvalidOperationException). Por isso os testes usam SQLite em memória
    // — um provider relacional de verdade — em vez do InMemory.
    //
    // O banco ":memory:" do SQLite só existe enquanto a conexão que o abriu
    // continua aberta; por isso mantemos uma SqliteConnection viva durante
    // toda a vida do teste (IDisposable) em vez de deixar o EF abrir/fechar
    // conexões por conta própria.
    public sealed class SqliteContextFixture : IDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }

        public SqliteContextFixture()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new AppDbContext(options);
            Context.Database.EnsureCreated();

            // Product.UserId/Order.UserId têm FK pra AdminUsers — os testes
            // usam os ids 1 (usuário comum) e 2 (outro usuário comum) como
            // donos de cadastro; sem essas linhas, o SQLite (que, diferente
            // do Postgres aqui, teve a checagem de FK habilitada por
            // padrão) rejeita qualquer Product/Order com esses ids.
            Context.AdminUsers.AddRange(
                new AdminUser { Id = 1, Nome = "Usuário Teste", Email = "user1@teste.com", PasswordHash = "x" },
                new AdminUser { Id = 2, Nome = "Outro Usuário", Email = "user2@teste.com", PasswordHash = "x" }
            );
            Context.SaveChanges();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
