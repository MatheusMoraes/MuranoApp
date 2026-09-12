using MuranoApp.DTOs;
using MuranoApp.Models;
using MuranoApp.Services;
using Xunit;

namespace MuranoApp.Tests
{
    // xUnit cria uma instância nova da classe de teste para cada [Fact]/[Theory],
    // então instanciar a fixture aqui (em vez de via IClassFixture) já garante
    // um banco SQLite em memória isolado por teste, sem precisar limpar estado
    // manualmente entre eles.
    public class OrderServiceTests : IDisposable
    {
        private readonly SqliteContextFixture _fixture;
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            _fixture = new SqliteContextFixture();
            _service = new OrderService(_fixture.Context);
        }

        public void Dispose() => _fixture.Dispose();

        private Client AddClient(string nome = "Cliente Teste", bool comEndereco = true)
        {
            var client = new Client
            {
                Nome = nome,
                Cep = comEndereco ? "12345-000" : null,
                Rua = comEndereco ? "Rua das Flores" : null,
                Bairro = comEndereco ? "Centro" : null,
                Cidade = comEndereco ? "São Paulo" : null,
                Estado = comEndereco ? "SP" : null,
                Numero = comEndereco ? "100" : null
            };
            _fixture.Context.Clients.Add(client);
            _fixture.Context.SaveChanges();
            return client;
        }

        private Category AddCategory(string nome = "Categoria Teste")
        {
            var category = new Category { Nome = nome, NomeNormalizado = NameNormalizer.Normalize(nome) };
            _fixture.Context.Categories.Add(category);
            _fixture.Context.SaveChanges();
            return category;
        }

        private Product AddProduct(
            Category categoria,
            string nome = "Produto Teste",
            decimal precoVarejo = 10m,
            decimal? precoAtacado = null,
            int? quantidadeMinimaAtacado = null,
            int quantidade = 100)
        {
            var product = new Product
            {
                Nome = nome,
                NomeNormalizado = NameNormalizer.Normalize(nome),
                CategoriaId = categoria.Id,
                Categoria = categoria,
                PrecoVarejo = precoVarejo,
                PrecoAtacado = precoAtacado,
                QuantidadeMinimaAtacado = quantidadeMinimaAtacado,
                Quantidade = quantidade
            };
            _fixture.Context.Products.Add(product);
            _fixture.Context.SaveChanges();
            return product;
        }

        [Fact]
        public async Task CreateAsync_DecrementaEstoqueDoProduto()
        {
            var categoria = AddCategory();
            var cliente = AddClient();
            var produto = AddProduct(categoria, quantidade: 50);

            await _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = cliente.Id,
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 5 } }
            });

            var produtoAtualizado = _fixture.Context.Products.Find(produto.Id);
            Assert.Equal(45, produtoAtualizado!.Quantidade);
        }

        [Fact]
        public async Task CreateAsync_UsaPrecoVarejoQuandoAbaixoDoMinimoAtacado()
        {
            var categoria = AddCategory();
            var cliente = AddClient();
            var produto = AddProduct(categoria, precoVarejo: 10m, precoAtacado: 7m, quantidadeMinimaAtacado: 10);

            var result = await _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = cliente.Id,
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 3 } }
            });

            Assert.Equal(10m, result.Items[0].PrecoUnitario);
            Assert.Equal(30m, result.ValorTotal);
        }

        [Fact]
        public async Task CreateAsync_UsaPrecoAtacadoQuandoAtingeQuantidadeMinima()
        {
            var categoria = AddCategory();
            var cliente = AddClient();
            var produto = AddProduct(categoria, precoVarejo: 10m, precoAtacado: 7m, quantidadeMinimaAtacado: 10);

            var result = await _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = cliente.Id,
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 10 } }
            });

            Assert.Equal(7m, result.Items[0].PrecoUnitario);
            Assert.Equal(70m, result.ValorTotal);
        }

        [Fact]
        public async Task CreateAsync_LancaExcecaoQuandoEstoqueInsuficiente()
        {
            var categoria = AddCategory();
            var cliente = AddClient();
            var produto = AddProduct(categoria, quantidade: 2);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = cliente.Id,
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 5 } }
            }));
        }

        [Fact]
        public async Task CreateAsync_LancaExcecaoQuandoClienteNaoExiste()
        {
            var categoria = AddCategory();
            var produto = AddProduct(categoria);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = 999,
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 1 } }
            }));
        }

        [Fact]
        public async Task CreateAsync_LancaExcecaoQuandoProdutoNaoExiste()
        {
            var cliente = AddClient();

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = cliente.Id,
                Items = new List<OrderItemDTO> { new() { ProdutoId = 999, Quantidade = 1 } }
            }));
        }

        [Fact]
        public async Task CreateAsync_LancaExcecaoQuandoClienteSemEnderecoENenhumInformado()
        {
            var categoria = AddCategory();
            var cliente = AddClient(comEndereco: false);
            var produto = AddProduct(categoria);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = cliente.Id,
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 1 } }
            }));
        }

        [Fact]
        public async Task CreateAsync_UsaEnderecoInformadoQuandoClienteNaoTemCadastrado()
        {
            var categoria = AddCategory();
            var cliente = AddClient(comEndereco: false);
            var produto = AddProduct(categoria);

            var result = await _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = cliente.Id,
                EnderecoEntrega = new EnderecoDTO
                {
                    Cep = "99999-000",
                    Rua = "Rua Alternativa",
                    Bairro = "Bairro X",
                    Cidade = "Cidade Y",
                    Estado = "RJ",
                    Numero = "42"
                },
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 1 } }
            });

            Assert.Equal("Rua Alternativa", result.Rua);
            Assert.Equal("RJ", result.Estado);
        }

        [Fact]
        public async Task DeleteAsync_RestauraEstoqueDosItens()
        {
            var categoria = AddCategory();
            var cliente = AddClient();
            var produto = AddProduct(categoria, quantidade: 50);

            var created = await _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = cliente.Id,
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 10 } }
            });

            await _service.DeleteAsync(created.Id);

            var produtoAtualizado = _fixture.Context.Products.Find(produto.Id);
            Assert.Equal(50, produtoAtualizado!.Quantidade);
        }

        [Fact]
        public async Task DeleteAsync_NaoQuebraQuandoProdutoDoItemJaFoiExcluido()
        {
            var categoria = AddCategory();
            var cliente = AddClient();
            var produto = AddProduct(categoria, quantidade: 50);

            var created = await _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = cliente.Id,
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 10 } }
            });

            // Simula a exclusão do produto: SetNull deixaria ProdutoId nulo no
            // OrderItem (comportamento configurado no AppDbContext); aqui
            // reproduzimos isso diretamente pra não depender de cascata do
            // SQLite em memória. Recarrega o produto em vez de reusar a
            // referência local — CreateAsync já limpou o ChangeTracker e
            // recarregou sua própria instância, então remover a instância
            // antiga geraria conflito de tracking (duas instâncias com a
            // mesma chave).
            var produtoTracked = _fixture.Context.Products.Find(produto.Id);
            _fixture.Context.Products.Remove(produtoTracked!);
            var item = _fixture.Context.OrderItems.First(i => i.OrderId == created.Id);
            item.ProdutoId = null;
            _fixture.Context.SaveChanges();

            // Não deve lançar mesmo sem produto pra repor estoque.
            await _service.DeleteAsync(created.Id);

            Assert.Null(_fixture.Context.Orders.Find(created.Id));
        }

        [Fact]
        public async Task UpdateAsync_AjustaEstoqueEntreVersaoAntigaENova()
        {
            var categoria = AddCategory();
            var cliente = AddClient();
            var produto = AddProduct(categoria, quantidade: 50);

            var created = await _service.CreateAsync(new CreateOrderDTO
            {
                ClientId = cliente.Id,
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 10 } }
            });
            // 50 - 10 = 40 em estoque agora.

            await _service.UpdateAsync(created.Id, new CreateOrderDTO
            {
                ClientId = cliente.Id,
                Items = new List<OrderItemDTO> { new() { ProdutoId = produto.Id, Quantidade = 3 } }
            });
            // Restaura os 10 (volta a 50) e decrementa 3 (fica em 47).

            var produtoAtualizado = _fixture.Context.Products.Find(produto.Id);
            Assert.Equal(47, produtoAtualizado!.Quantidade);
        }
    }
}
