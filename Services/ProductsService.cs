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

            if (dto.PrecoVarejo < 0)
                throw new ArgumentException("Price cannot be negative.");

            ValidatePrecoAtacado(dto.PrecoVarejo, dto.PrecoAtacado, dto.QuantidadeMinimaAtacado);

            var product = new Models.Product
            {
                Nome = dto.Nome,
                PrecoVarejo = dto.PrecoVarejo,
                PrecoAtacado = dto.PrecoAtacado,
                QuantidadeMinimaAtacado = dto.QuantidadeMinimaAtacado,
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

            ValidatePrecoAtacado(dto.PrecoVarejo, dto.PrecoAtacado, dto.QuantidadeMinimaAtacado);

            product.Nome = dto.Nome;
            product.PrecoVarejo = dto.PrecoVarejo;
            product.PrecoAtacado = dto.PrecoAtacado;
            product.QuantidadeMinimaAtacado = dto.QuantidadeMinimaAtacado;
            product.Quantidade = dto.Quantidade;

            await _context.SaveChangesAsync();

            return true;
        }

        // Preço e quantidade mínima de atacado só fazem sentido juntos, e o
        // preço de atacado deve representar um desconto real sobre o varejo.
        private static void ValidatePrecoAtacado(decimal precoVarejo, decimal? precoAtacado, int? quantidadeMinimaAtacado)
        {
            if (precoAtacado.HasValue != quantidadeMinimaAtacado.HasValue)
                throw new ArgumentException(
                    "PrecoAtacado e QuantidadeMinimaAtacado devem ser informados juntos.");

            if (!precoAtacado.HasValue)
                return;

            if (precoAtacado.Value < 0)
                throw new ArgumentException("Wholesale price cannot be negative.");

            if (quantidadeMinimaAtacado!.Value <= 0)
                throw new ArgumentException("Wholesale minimum quantity must be greater than zero.");

            if (precoAtacado.Value > precoVarejo)
                throw new ArgumentException("Wholesale price must not be greater than retail price.");
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

        private ProductResponseDTO ToResponse(Models.Product product)
        {
            return new ProductResponseDTO
            {
                Id = product.Id,
                Nome = product.Nome,
                PrecoVarejo = product.PrecoVarejo,
                PrecoAtacado = product.PrecoAtacado,
                QuantidadeMinimaAtacado = product.QuantidadeMinimaAtacado,
                Quantidade = product.Quantidade,
                CriadoEm = product.CriadoEm
            };
        }
    }
}
