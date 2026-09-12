using MuranoApp.DTOs;
using MuranoApp.Models;
using MuranoApp.Services;
using Xunit;

namespace MuranoApp.Tests
{
    public class CategoryServiceTests : IDisposable
    {
        private readonly SqliteContextFixture _fixture;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            _fixture = new SqliteContextFixture();
            _service = new CategoryService(_fixture.Context);
        }

        public void Dispose() => _fixture.Dispose();

        [Fact]
        public async Task CreateAsync_PreenchoNomeNormalizado()
        {
            var result = await _service.CreateAsync(new CreateCategoryDTO { Nome = "Colares  Especiais" });

            var salvo = _fixture.Context.Categories.Find(result.Id);
            Assert.Equal("colares especiais", salvo!.NomeNormalizado);
        }

        [Fact]
        public async Task CreateAsync_RejeitaNomeDuplicadoIgnorandoCaixaEEspacos()
        {
            await _service.CreateAsync(new CreateCategoryDTO { Nome = "Pulseiras" });

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(new CreateCategoryDTO { Nome = "  PULSEIRAS  " }));
        }

        [Fact]
        public async Task DeleteAsync_RejeitaExclusaoQuandoTemProdutos()
        {
            var categoria = await _service.CreateAsync(new CreateCategoryDTO { Nome = "Com Produtos" });
            _fixture.Context.Products.Add(new Product
            {
                Nome = "Produto X",
                NomeNormalizado = "produto x",
                CategoriaId = categoria.Id,
                PrecoVarejo = 5m,
                Quantidade = 1
            });
            _fixture.Context.SaveChanges();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(categoria.Id));
        }

        [Fact]
        public async Task DeleteAsync_PermiteExclusaoQuandoSemProdutos()
        {
            var categoria = await _service.CreateAsync(new CreateCategoryDTO { Nome = "Sem Produtos" });

            var deleted = await _service.DeleteAsync(categoria.Id);

            Assert.True(deleted);
        }
    }
}
