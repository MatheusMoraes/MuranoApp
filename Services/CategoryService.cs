using Microsoft.EntityFrameworkCore;
using MuranoApp.Data;
using MuranoApp.DTOs;
using MuranoApp.Models;
using Npgsql;

namespace MuranoApp.Services
{
    public class CategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CategoryResponseDTO> CreateAsync(CreateCategoryDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new ArgumentException("Name is required.");

            await EnsureNomeIsUniqueAsync(dto.Nome, excludeId: null);

            var category = new Category
            {
                Nome = dto.Nome,
                NomeNormalizado = NameNormalizer.Normalize(dto.Nome),
                Descricao = dto.Descricao
            };

            _context.Categories.Add(category);
            await TrySaveOrThrowDuplicateAsync(dto.Nome);

            return ToResponse(category);
        }

        public async Task<CategoryResponseDTO?> GetByIdAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                return null;

            return ToResponse(category);
        }

        public async Task<List<CategoryResponseDTO>> GetAllAsync()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Nome)
                .ToListAsync();

            return categories.Select(ToResponse).ToList();
        }

        public async Task<bool> UpdateAsync(int id, UpdateCategoryDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new ArgumentException("Name is required.");

            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                return false;

            await EnsureNomeIsUniqueAsync(dto.Nome, excludeId: id);

            category.Nome = dto.Nome;
            category.NomeNormalizado = NameNormalizer.Normalize(dto.Nome);
            category.Descricao = dto.Descricao;

            await TrySaveOrThrowDuplicateAsync(dto.Nome);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return false;

            if (category.Products.Count > 0)
                throw new InvalidOperationException(
                    "Cannot delete a category that has products.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return true;
        }

        // Mesma regra usada pra nome de produto: ignora maiúsculas/minúsculas
        // e diferenças de espaçamento, e usa o índice único (NomeNormalizado)
        // em vez de carregar a tabela inteira. TrySaveOrThrowDuplicateAsync é
        // a rede de segurança contra corrida.
        private async Task EnsureNomeIsUniqueAsync(string nome, int? excludeId)
        {
            var normalizado = NameNormalizer.Normalize(nome);

            var duplicado = await _context.Categories
                .AnyAsync(c => c.NomeNormalizado == normalizado && (excludeId == null || c.Id != excludeId));

            if (duplicado)
                throw new ArgumentException($"A category named \"{nome.Trim()}\" already exists.");
        }

        private async Task TrySaveOrThrowDuplicateAsync(string nome)
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw new ArgumentException($"A category named \"{nome.Trim()}\" already exists.");
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException { SqlState: "23505" };
        }

        private static CategoryResponseDTO ToResponse(Category category)
        {
            return new CategoryResponseDTO
            {
                Id = category.Id,
                Nome = category.Nome,
                Descricao = category.Descricao,
                CriadoEm = category.CriadoEm
            };
        }
    }
}
