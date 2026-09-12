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
    }
}
