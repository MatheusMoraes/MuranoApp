using MuranoApp.Models;
using MuranoApp.Services;
using Xunit;

namespace MuranoApp.Tests
{
    public class DashboardServiceTests : IDisposable
    {
        private readonly SqliteContextFixture _fixture;
        private readonly DashboardService _service;

        public DashboardServiceTests()
        {
            _fixture = new SqliteContextFixture();
            _service = new DashboardService(_fixture.Context);
        }

        public void Dispose() => _fixture.Dispose();

        [Fact]
        public async Task GetAsync_SemPedidos_RetornaZerados()
        {
            var result = await _service.GetAsync();

            Assert.Equal(0, result.TotalPedidos);
            Assert.Equal(0m, result.ReceitaTotal);
            Assert.Equal(0m, result.TicketMedio);
            Assert.Empty(result.TopClientes);
            Assert.Empty(result.TopProdutos);
        }

        [Fact]
        public async Task GetAsync_RankeiaClientePorQuantidadeDePedidos()
        {
            var categoria = new Category { Nome = "Cat", NomeNormalizado = "cat" };
            var produto = new Product
            {
                Nome = "Prod",
                NomeNormalizado = "prod",
                Categoria = categoria,
                PrecoVarejo = 10m,
                Quantidade = 100
            };
            var clienteA = new Client { Nome = "Cliente A" };
            var clienteB = new Client { Nome = "Cliente B" };
            _fixture.Context.AddRange(categoria, produto, clienteA, clienteB);
            _fixture.Context.SaveChanges();

            // Cliente A: 2 pedidos, Cliente B: 1 pedido.
            _fixture.Context.Orders.AddRange(
                new Order { ClientId = clienteA.Id, ValorTotal = 10m, Cep = "", Rua = "", Bairro = "", Cidade = "", Estado = "", Numero = "", Complemento = "" },
                new Order { ClientId = clienteA.Id, ValorTotal = 20m, Cep = "", Rua = "", Bairro = "", Cidade = "", Estado = "", Numero = "", Complemento = "" },
                new Order { ClientId = clienteB.Id, ValorTotal = 5m, Cep = "", Rua = "", Bairro = "", Cidade = "", Estado = "", Numero = "", Complemento = "" }
            );
            _fixture.Context.SaveChanges();

            var result = await _service.GetAsync();

            Assert.Equal(3, result.TotalPedidos);
            Assert.Equal(35m, result.ReceitaTotal);
            Assert.Equal("Cliente A", result.TopClientes[0].Nome);
            Assert.Equal(2, result.TopClientes[0].TotalPedidos);
        }

        [Fact]
        public async Task GetAsync_ContinuaContandoProdutoExcluidoViaNomeProduto()
        {
            var categoria = new Category { Nome = "Cat", NomeNormalizado = "cat" };
            var produto = new Product
            {
                Nome = "Produto Descontinuado",
                NomeNormalizado = "produto descontinuado",
                Categoria = categoria,
                PrecoVarejo = 10m,
                Quantidade = 100
            };
            var cliente = new Client { Nome = "Cliente" };
            _fixture.Context.AddRange(categoria, produto, cliente);
            _fixture.Context.SaveChanges();

            var order = new Order
            {
                ClientId = cliente.Id,
                ValorTotal = 50m,
                Cep = "", Rua = "", Bairro = "", Cidade = "", Estado = "", Numero = "", Complemento = ""
            };
            order.Items.Add(new OrderItem
            {
                // Simula item cujo produto já foi excluído (SetNull).
                ProdutoId = null,
                NomeProduto = "Produto Descontinuado",
                Quantidade = 5,
                PrecoUnitario = 10m
            });
            _fixture.Context.Orders.Add(order);
            _fixture.Context.SaveChanges();

            var result = await _service.GetAsync();

            Assert.Contains(result.TopProdutos, p => p.Nome == "Produto Descontinuado" && p.QuantidadeVendida == 5);
        }

        [Fact]
        public async Task GetAsync_ProdutosMaisCarosVendidos_OrdenaPorPrecoEExcluiOsSemVenda()
        {
            var categoria = new Category { Nome = "Cat", NomeNormalizado = "cat" };
            var caro = new Product { Nome = "Caro", NomeNormalizado = "caro", Categoria = categoria, PrecoVarejo = 100m, Quantidade = 50 };
            var barato = new Product { Nome = "Barato", NomeNormalizado = "barato", Categoria = categoria, PrecoVarejo = 10m, Quantidade = 50 };
            var caroSemVenda = new Product { Nome = "Caro Parado", NomeNormalizado = "caro parado", Categoria = categoria, PrecoVarejo = 500m, Quantidade = 50 };
            var cliente = new Client { Nome = "Cliente" };
            _fixture.Context.AddRange(categoria, caro, barato, caroSemVenda, cliente);
            _fixture.Context.SaveChanges();

            var order = new Order
            {
                ClientId = cliente.Id,
                ValorTotal = 220m,
                Cep = "", Rua = "", Bairro = "", Cidade = "", Estado = "", Numero = "", Complemento = ""
            };
            order.Items.Add(new OrderItem { ProdutoId = caro.Id, NomeProduto = caro.Nome, Quantidade = 2, PrecoUnitario = 100m });
            order.Items.Add(new OrderItem { ProdutoId = barato.Id, NomeProduto = barato.Nome, Quantidade = 2, PrecoUnitario = 10m });
            _fixture.Context.Orders.Add(order);
            _fixture.Context.SaveChanges();

            var result = await _service.GetAsync();

            // "Caro Parado" nunca vendeu — não deve entrar no ranking mesmo
            // sendo o mais caro do catálogo.
            Assert.DoesNotContain(result.ProdutosMaisCarosVendidos, p => p.Nome == "Caro Parado");
            Assert.Equal("Caro", result.ProdutosMaisCarosVendidos[0].Nome);
            Assert.Equal(2, result.ProdutosMaisCarosVendidos[0].QuantidadeVendida);
            Assert.Equal("Barato", result.ProdutosMaisCarosVendidos[1].Nome);
        }

        private Client AddClientDireto()
        {
            var client = new Client { Nome = "Cliente" };
            _fixture.Context.Clients.Add(client);
            _fixture.Context.SaveChanges();
            return client;
        }

        private void AddOrderEm(int clientId, DateTime criadoEm, decimal valorTotal)
        {
            _fixture.Context.Orders.Add(new Order
            {
                ClientId = clientId,
                ValorTotal = valorTotal,
                CriadoEm = criadoEm,
                Cep = "", Rua = "", Bairro = "", Cidade = "", Estado = "", Numero = "", Complemento = ""
            });
            _fixture.Context.SaveChanges();
        }

        [Fact]
        public async Task GetRevenueByPeriodAsync_PeriodoInvalido_LancaArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetRevenueByPeriodAsync("2anos"));
        }

        [Fact]
        public async Task GetRevenueByPeriodAsync_30d_AgrupaPorDiaEIgnoraPedidosForaDoIntervalo()
        {
            var cliente = AddClientDireto();
            var hoje = DateTime.UtcNow.Date;

            AddOrderEm(cliente.Id, hoje, 10m);
            AddOrderEm(cliente.Id, hoje.AddDays(-1), 5m);
            // Fora da janela de 30 dias — não deve entrar na soma.
            AddOrderEm(cliente.Id, hoje.AddDays(-40), 999m);

            var result = await _service.GetRevenueByPeriodAsync("30d");

            Assert.Equal("dia", result.Granularidade);
            Assert.Equal(30, result.Pontos.Count);
            Assert.Equal(15m, result.ReceitaTotal);
            Assert.Equal(10m, result.Pontos.Last().Receita);
            Assert.Equal(hoje, result.Pontos.Last().Data);
        }

        [Fact]
        public async Task GetRevenueByPeriodAsync_CalculaVariacaoComPeriodoAnterior()
        {
            var cliente = AddClientDireto();
            var hoje = DateTime.UtcNow.Date;

            // Período atual (30d): receita 200. Período anterior (30 dias
            // antes desses): receita 100 — variação de +100%.
            AddOrderEm(cliente.Id, hoje, 200m);
            AddOrderEm(cliente.Id, hoje.AddDays(-35), 100m);

            var result = await _service.GetRevenueByPeriodAsync("30d");

            Assert.Equal(200m, result.ReceitaTotal);
            Assert.Equal(1, result.TotalPedidos);
            Assert.Equal(200m, result.TicketMedio);
            Assert.Equal(100m, result.ReceitaPeriodoAnterior);
            Assert.Equal(100m, result.VariacaoPercentual);
        }

        [Fact]
        public async Task GetRevenueByPeriodAsync_SemReceitaNoPeriodoAnterior_VariacaoFicaNula()
        {
            var cliente = AddClientDireto();
            AddOrderEm(cliente.Id, DateTime.UtcNow.Date, 50m);

            var result = await _service.GetRevenueByPeriodAsync("30d");

            Assert.Null(result.VariacaoPercentual);
        }

        [Fact]
        public async Task GetRevenueByPeriodAsync_Ano_AgrupaPorMes()
        {
            var cliente = AddClientDireto();
            var hoje = DateTime.UtcNow.Date;

            AddOrderEm(cliente.Id, hoje, 100m);
            AddOrderEm(cliente.Id, hoje.AddMonths(-2), 50m);

            var result = await _service.GetRevenueByPeriodAsync("ano");

            Assert.Equal("mes", result.Granularidade);
            Assert.Equal(150m, result.ReceitaTotal);
            // Um ponto por mês calendário dentro da janela — pelo menos os
            // dois meses com pedido devem aparecer com a soma correta.
            Assert.Contains(result.Pontos, p => p.Data.Year == hoje.Year && p.Data.Month == hoje.Month && p.Receita == 100m);
        }
    }
}
