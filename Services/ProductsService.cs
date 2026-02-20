using Microsoft.EntityFrameworkCore;
using MuranoApp.Data;
using MuranoApp.DTOs;
using MuranoApp.Models;

namespace MuranoApp.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProductResponseDTO> CreateAsync(CreateProductDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new ArgumentException("Name is required.");

            if (dto.Quantidade < 0)
                throw new ArgumentException("Stock cannot be negative.");

            if (dto.Preco < 0)
                throw new ArgumentException("Price cannot be negative.");

            var product = new Product
            {
                Nome = dto.Nome,
                Preco = dto.Preco,
                Quantidade = dto.Quantidade
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return ToResponse(product);
        }

        public async Task<ProductResponseDTO?> GetByIdAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return null;

            return ToResponse(product);
        }

        public async Task<List<ProductResponseDTO>> GetAllAsync()
        {
            var products = await _context.Products.ToListAsync();

            return products.Select(ToResponse).ToList();
        }

        public async Task<bool> UpdateAsync(int id, UpdateProductDTO dto)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return false;

            product.Nome = dto.Nome;
            product.Preco = dto.Preco;
            product.Quantidade = dto.Quantidade;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return true;
        }

        private ProductResponseDTO ToResponse(Product product)
        {
            return new ProductResponseDTO
            {
                Id = product.Id,
                Nome = product.Nome,
                Preco = product.Preco,
                Quantidade = product.Quantidade,
                CriadoEm = product.CriadoEm
            };
        }
    }
}
