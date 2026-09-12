using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MuranoApp.Data;

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
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
