using Microsoft.EntityFrameworkCore;
using MuranoApp.Data;
using MuranoApp.DTOs;
using MuranoApp.Models;

namespace MuranoApp.Services
{
    public class OrderService
    {
        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderResponseDTO> CreateAsync(CreateOrderDTO dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new ArgumentException("Order must contain at least one item.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            var order = new Order();
            decimal total = 0;

            foreach (var item in dto.Items)
            {
                if (item.Quantidade <= 0)
                    throw new ArgumentException("Quantity must be greater than zero.");

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProdutoId);

                if (product == null)
                    throw new KeyNotFoundException($"Product {item.ProdutoId} not found.");

                if (product.Quantidade < item.Quantidade)
                    throw new InvalidOperationException(
                        $"Insufficient stock for {product.Nome}");

                product.Quantidade -= item.Quantidade;

                var orderItem = new OrderItem
                {
                    ProdutoId = product.Id,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = product.Preco
                };

                total += item.Quantidade * product.Preco;

                order.Items.Add(orderItem);
            }

            order.ValorTotal = total;

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Reload with products for response
            var createdOrder = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Produto)
                .FirstAsync(o => o.Id == order.Id);

            return ToResponse(createdOrder);
        }

        public async Task<List<OrderResponseDTO>> GetAllAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Produto)
                .ToListAsync();

            return orders.Select(ToResponse).ToList();
        }

        public async Task<OrderResponseDTO> GetByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Produto)
                .FirstAsync(o => o.Id == id);

            return ToResponse(order);
        }

        public async Task DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                throw new KeyNotFoundException($"Order {id} not found.");

            // Restore stock
            foreach (var item in order.Items)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProdutoId);

                if (product != null)
                {
                    product.Quantidade += item.Quantidade;
                }
            }

            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task<OrderResponseDTO> UpdateAsync(int id, CreateOrderDTO dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new ArgumentException("Order must contain at least one item.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                throw new KeyNotFoundException($"Order {id} not found.");

            // Restore stock from existing items
            foreach (var existingItem in order.Items)
            {
                var productToRestore = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == existingItem.ProdutoId);

                if (productToRestore != null)
                {
                    productToRestore.Quantidade += existingItem.Quantidade;
                }
            }

            // Remove existing items
            _context.OrderItems.RemoveRange(order.Items);
            order.Items.Clear();
            order.ValorTotal = 0;

            decimal total = 0;

            // Apply new items
            foreach (var item in dto.Items)
            {
                if (item.Quantidade <= 0)
                    throw new ArgumentException("Quantity must be greater than zero.");

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProdutoId);

                if (product == null)
                    throw new KeyNotFoundException($"Product {item.ProdutoId} not found.");

                if (product.Quantidade < item.Quantidade)
                    throw new InvalidOperationException(
                        $"Insufficient stock for {product.Nome}");

                product.Quantidade -= item.Quantidade;

                var orderItem = new OrderItem
                {
                    ProdutoId = product.Id,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = product.Preco
                };

                total += item.Quantidade * product.Preco;

                order.Items.Add(orderItem);
            }

            order.ValorTotal = total;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var updatedOrder = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Produto)
                .FirstAsync(o => o.Id == order.Id);

            return ToResponse(updatedOrder);
        }

        private OrderResponseDTO ToResponse(Order order)
        {
            return new OrderResponseDTO
            {
                Id = order.Id,
                CriadoEm = order.CriadoEm,
                ValorTotal = order.ValorTotal,
                Items = order.Items.Select(i => new OrderItemResponseDTO
                {
                    ProdutoId = i.ProdutoId,
                    NomeProduto = i.Produto.Nome,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario
                }).ToList()
            };
        }
    }
}
