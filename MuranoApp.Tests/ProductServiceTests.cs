using MuranoApp.DTOs;
using MuranoApp.Models;
using MuranoApp.Services;
using Xunit;

namespace MuranoApp.Tests
{
    public class ProductServiceTests : IDisposable
    {
        private const int UserId = 1;
        private const int OtherUserId = 2;

        private readonly SqliteContextFixture _fixture;
        private readonly ProductService _service;

        public ProductServiceTests()
        {
            _fixture = new SqliteContextFixture();
            _service = new ProductService(_fixture.Context);
        }

        public void Dispose() => _fixture.Dispose();

        private Category AddCategory(string nome = "Categoria Teste")
        {
            var category = new Category { Nome = nome, NomeNormalizado = NameNormalizer.Normalize(nome) };
            _fixture.Context.Categories.Add(category);
            _fixture.Context.SaveChanges();
            return category;
        }

        private static CreateProductDTO BuildDto(
            string nome,
            int categoriaId,
            decimal precoVarejo = 10m,
            decimal? precoAtacado = null,
            int? quantidadeMinimaAtacado = null) => new()
            {
                Nome = nome,
                CategoriaId = categoriaId,
                PrecoVarejo = precoVarejo,
                PrecoAtacado = precoAtacado,
                QuantidadeMinimaAtacado = quantidadeMinimaAtacado,
                Quantidade = 10
            };

        [Fact]
        public async Task CreateAsync_PreenchoNomeNormalizado()
        {
            var categoria = AddCategory();

            var result = await _service.CreateAsync(BuildDto("Colar de Miçangas", categoria.Id), UserId);

            var salvo = _fixture.Context.Products.Find(result.Id);
            Assert.Equal("colar de miçangas", salvo!.NomeNormalizado);
        }

        // Índice único (UserId, NomeNormalizado) é quem garante isso no banco
        // — este teste cobre a validação em memória que dá a mensagem
        // amigável antes de chegar lá (ver EnsureNomeIsUniqueAsync).
        [Theory]
        [InlineData("Pulseira Azul", "PULSEIRA   Azul")]
        [InlineData("Pulseira Azul", "  pulseira azul  ")]
        public async Task CreateAsync_RejeitaNomeDuplicadoIgnorandoCaixaEEspacos(string original, string duplicado)
        {
            var categoria = AddCategory();
            await _service.CreateAsync(BuildDto(original, categoria.Id), UserId);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(BuildDto(duplicado, categoria.Id), UserId));
        }

        [Fact]
        public async Task CreateAsync_PermiteMesmoNomeEmCatalogosDeUsuariosDiferentes()
        {
            var categoria = AddCategory();
            await _service.CreateAsync(BuildDto("Pulseira Azul", categoria.Id), UserId);

            // Mesmo nome, outro usuário — catálogos separados, não é duplicata.
            var result = await _service.CreateAsync(BuildDto("Pulseira Azul", categoria.Id), OtherUserId);

            Assert.NotEqual(0, result.Id);
        }

        [Fact]
        public async Task UpdateAsync_PermiteManterOProprioNome()
        {
            var categoria = AddCategory();
            var created = await _service.CreateAsync(BuildDto("Anel Dourado", categoria.Id), UserId);

            var updated = await _service.UpdateAsync(created.Id, new UpdateProductDTO
            {
                Nome = "Anel Dourado",
                CategoriaId = categoria.Id,
                PrecoVarejo = 15m,
                Quantidade = 20
            }, UserId, isAdmin: false);

            Assert.True(updated);
        }

        [Fact]
        public async Task UpdateAsync_RejeitaRenomearParaNomeDeOutroProduto()
        {
            var categoria = AddCategory();
            await _service.CreateAsync(BuildDto("Brinco Prata", categoria.Id), UserId);
            var outro = await _service.CreateAsync(BuildDto("Colar Prata", categoria.Id), UserId);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(outro.Id, new UpdateProductDTO
            {
                Nome = "Brinco Prata",
                CategoriaId = categoria.Id,
                PrecoVarejo = 15m,
                Quantidade = 20
            }, UserId, isAdmin: false));
        }

        [Fact]
        public async Task CreateAsync_RejeitaPrecoAtacadoMaiorQueVarejo()
        {
            var categoria = AddCategory();

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(
                BuildDto("Produto Caro", categoria.Id, precoVarejo: 10m, precoAtacado: 15m, quantidadeMinimaAtacado: 5), UserId));
        }

        [Fact]
        public async Task CreateAsync_RejeitaPrecoAtacadoSemQuantidadeMinima()
        {
            var categoria = AddCategory();

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(
                BuildDto("Produto Incompleto", categoria.Id, precoVarejo: 10m, precoAtacado: 7m, quantidadeMinimaAtacado: null), UserId));
        }

        [Fact]
        public async Task CreateAsync_LancaExcecaoQuandoCategoriaNaoExiste()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.CreateAsync(BuildDto("Produto Órfão", categoriaId: 999), UserId));
        }

        [Fact]
        public async Task GetByIdAsync_UsuarioComumNaoVeProdutoDeOutroUsuario()
        {
            var categoria = AddCategory();
            var produto = await _service.CreateAsync(BuildDto("Produto do Outro", categoria.Id), OtherUserId);

            var result = await _service.GetByIdAsync(produto.Id, UserId, isAdmin: false);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_AdminVeProdutoDeQualquerUsuario()
        {
            var categoria = AddCategory();
            var produto = await _service.CreateAsync(BuildDto("Produto do Outro", categoria.Id), OtherUserId);

            var result = await _service.GetByIdAsync(produto.Id, UserId, isAdmin: true);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetAllAsync_UsuarioComumSoVeOsProprios()
        {
            var categoria = AddCategory();
            await _service.CreateAsync(BuildDto("Meu Produto", categoria.Id), UserId);
            await _service.CreateAsync(BuildDto("Produto do Outro", categoria.Id), OtherUserId);

            var result = await _service.GetAllAsync(UserId, isAdmin: false);

            Assert.Single(result);
            Assert.Equal("Meu Produto", result[0].Nome);
        }

        [Fact]
        public async Task GetAllAsync_AdminVeTodos()
        {
            var categoria = AddCategory();
            await _service.CreateAsync(BuildDto("Meu Produto", categoria.Id), UserId);
            await _service.CreateAsync(BuildDto("Produto do Outro", categoria.Id), OtherUserId);

            var result = await _service.GetAllAsync(UserId, isAdmin: true);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task UpdateAsync_UsuarioComumNaoConseguAlterarProdutoDeOutroUsuario()
        {
            var categoria = AddCategory();
            var produto = await _service.CreateAsync(BuildDto("Produto do Outro", categoria.Id), OtherUserId);

            var updated = await _service.UpdateAsync(produto.Id, new UpdateProductDTO
            {
                Nome = "Produto do Outro",
                CategoriaId = categoria.Id,
                PrecoVarejo = 99m,
                Quantidade = 1
            }, UserId, isAdmin: false);

            Assert.False(updated);
        }

        [Fact]
        public async Task DeleteAsync_UsuarioComumNaoConsegueExcluirProdutoDeOutroUsuario()
        {
            var categoria = AddCategory();
            var produto = await _service.CreateAsync(BuildDto("Produto do Outro", categoria.Id), OtherUserId);

            var deleted = await _service.DeleteAsync(produto.Id, UserId, isAdmin: false);

            Assert.False(deleted);
        }
    }
}
