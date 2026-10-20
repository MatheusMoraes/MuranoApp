using Microsoft.EntityFrameworkCore;
using MuranoApp.Data;
using MuranoApp.DTOs;
using MuranoApp.Models;
using Npgsql;

namespace MuranoApp.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;
        // Opcional: só é usado pra apagar imagens órfãs na Cloudinary quando
        // um produto troca/perde a imagem ou é excluído. Sem ele, essas
        // limpezas simplesmente não acontecem (ex: chamadas que não mexem
        // com imagem não precisam instanciar isso).
        private readonly CloudinaryService? _cloudinaryService;

        public ProductService(AppDbContext context, CloudinaryService? cloudinaryService = null)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ProductResponseDTO> CreateAsync(CreateProductDTO dto, int userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new ArgumentException("Name is required.");

            if (dto.Quantidade < 0)
                throw new ArgumentException("Stock cannot be negative.");

            if (dto.PrecoVarejo < 0)
                throw new ArgumentException("Price cannot be negative.");

            if (dto.EstoqueMinimo is < 0)
                throw new ArgumentException("Minimum stock cannot be negative.");

            ValidatePrecoAtacado(dto.PrecoVarejo, dto.PrecoAtacado, dto.QuantidadeMinimaAtacado);
            await EnsureNomeIsUniqueAsync(dto.Nome, userId, excludeId: null);

            var categoria = await _context.Categories.FindAsync(dto.CategoriaId);
            if (categoria == null)
                throw new KeyNotFoundException($"Category {dto.CategoriaId} not found.");

            var product = new Models.Product
            {
                Nome = dto.Nome,
                NomeNormalizado = NameNormalizer.Normalize(dto.Nome),
                CategoriaId = dto.CategoriaId,
                Categoria = categoria,
                PrecoVarejo = dto.PrecoVarejo,
                PrecoAtacado = dto.PrecoAtacado,
                QuantidadeMinimaAtacado = dto.QuantidadeMinimaAtacado,
                Quantidade = dto.Quantidade,
                EstoqueMinimo = dto.EstoqueMinimo,
                ImagemUrl = dto.ImagemUrl,
                ImagemPublicId = dto.ImagemPublicId,
                UserId = userId
            };

            _context.Products.Add(product);
            await TrySaveOrThrowDuplicateAsync(dto.Nome);

            return ToResponse(product);
        }

        // isAdmin ignora o dono — admin ("admin murano") vê qualquer
        // produto; usuário comum só vê o que ele mesmo cadastrou. Produto de
        // outro usuário, pra quem não é admin, é tratado como inexistente
        // (não "403 Forbidden") — não revela nem que o registro existe.
        public async Task<ProductResponseDTO?> GetByIdAsync(int id, int userId, bool isAdmin)
        {
            var product = await _context.Products
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == userId));

            if (product == null)
                return null;

            return ToResponse(product);
        }

        public async Task<List<ProductResponseDTO>> GetAllAsync(int userId, bool isAdmin)
        {
            var products = await _context.Products
                .Include(p => p.Categoria)
                .Where(p => isAdmin || p.UserId == userId)
                .ToListAsync();

            return products.Select(ToResponse).ToList();
        }

        public async Task<bool> UpdateAsync(int id, UpdateProductDTO dto, int userId, bool isAdmin)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == userId));

            if (product == null)
                return false;

            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new ArgumentException("Name is required.");

            if (dto.EstoqueMinimo is < 0)
                throw new ArgumentException("Minimum stock cannot be negative.");

            var categoriaExiste = await _context.Categories.AnyAsync(c => c.Id == dto.CategoriaId);
            if (!categoriaExiste)
                throw new KeyNotFoundException($"Category {dto.CategoriaId} not found.");

            ValidatePrecoAtacado(dto.PrecoVarejo, dto.PrecoAtacado, dto.QuantidadeMinimaAtacado);
            await EnsureNomeIsUniqueAsync(dto.Nome, product.UserId, excludeId: id);

            // Se a imagem mudou (trocou ou foi removida), a antiga vira
            // lixo na Cloudinary — apaga antes de sobrescrever a referência.
            if (!string.IsNullOrEmpty(product.ImagemPublicId) &&
                product.ImagemPublicId != dto.ImagemPublicId)
            {
                await TryDeleteImageAsync(product.ImagemPublicId);
            }

            product.Nome = dto.Nome;
            product.NomeNormalizado = NameNormalizer.Normalize(dto.Nome);
            product.CategoriaId = dto.CategoriaId;
            product.PrecoVarejo = dto.PrecoVarejo;
            product.PrecoAtacado = dto.PrecoAtacado;
            product.QuantidadeMinimaAtacado = dto.QuantidadeMinimaAtacado;
            product.Quantidade = dto.Quantidade;
            product.EstoqueMinimo = dto.EstoqueMinimo;
            product.ImagemUrl = dto.ImagemUrl;
            product.ImagemPublicId = dto.ImagemPublicId;

            await TrySaveOrThrowDuplicateAsync(dto.Nome);

            return true;
        }

        // Best-effort: um erro ao limpar a imagem antiga na Cloudinary não
        // pode impedir o produto de ser salvo/excluído.
        private async Task TryDeleteImageAsync(string publicId)
        {
            if (_cloudinaryService == null)
                return;

            try
            {
                await _cloudinaryService.DeleteImageAsync(publicId);
            }
            catch
            {
                // ignora — o pior caso é um arquivo órfão na Cloudinary.
            }
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

        // Nomes duplicados não são permitidos dentro do catálogo do mesmo
        // usuário (dois usuários podem ter um produto de mesmo nome em
        // catálogos separados), ignorando maiúsculas/minúsculas e espaços
        // ("batata quente" == "BaTaTa   QUENTE"). Checagem via índice único
        // (UserId, NomeNormalizado) em vez de carregar a tabela inteira — a
        // validação abaixo cobre o caso comum com mensagem amigável;
        // TrySaveOrThrowDuplicateAsync é a rede de segurança contra corrida,
        // que só o índice do banco consegue pegar de fato.
        private async Task EnsureNomeIsUniqueAsync(string nome, int userId, int? excludeId)
        {
            var normalizado = NameNormalizer.Normalize(nome);

            var duplicado = await _context.Products
                .AnyAsync(p => p.UserId == userId && p.NomeNormalizado == normalizado && (excludeId == null || p.Id != excludeId));

            if (duplicado)
                throw new ArgumentException($"A product named \"{nome.Trim()}\" already exists.");
        }

        // Rede de segurança: se duas requisições passarem pela checagem acima
        // ao mesmo tempo (corrida), o índice único do Postgres rejeita a
        // segunda gravação — aqui a exceção genérica do EF vira uma
        // ArgumentException amigável, igual à validação em memória.
        private async Task TrySaveOrThrowDuplicateAsync(string nome)
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw new ArgumentException($"A product named \"{nome.Trim()}\" already exists.");
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException { SqlState: "23505" };
        }

        public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == userId));

            if (product == null)
                return false;

            if (!string.IsNullOrEmpty(product.ImagemPublicId))
                await TryDeleteImageAsync(product.ImagemPublicId);

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
                CategoriaId = product.CategoriaId,
                CategoriaNome = product.Categoria?.Nome ?? string.Empty,
                PrecoVarejo = product.PrecoVarejo,
                PrecoAtacado = product.PrecoAtacado,
                QuantidadeMinimaAtacado = product.QuantidadeMinimaAtacado,
                Quantidade = product.Quantidade,
                EstoqueMinimo = product.EstoqueMinimo,
                EstoqueBaixo = product.EstoqueMinimo.HasValue && product.Quantidade <= product.EstoqueMinimo.Value,
                ImagemUrl = product.ImagemUrl,
                ImagemPublicId = product.ImagemPublicId,
                CriadoEm = product.CriadoEm
            };
        }
    }
}
