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

            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == dto.ClientId);

            if (client == null)
                throw new KeyNotFoundException($"Client {dto.ClientId} not found.");

            // EnableRetryOnFailure exige que transações manuais sejam executadas
            // através da execution strategy, senão o Npgsql lança
            // InvalidOperationException ao tentar abrir a transação.
            var strategy = _context.Database.CreateExecutionStrategy();

            var createdOrderId = await strategy.ExecuteAsync(async () =>
            {
                // Descarta qualquer estado rastreado de uma tentativa anterior
                // (retry), evitando decrementar o estoque em dobro.
                _context.ChangeTracker.Clear();

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

                    var precoUnitario = ResolvePrecoUnitario(product, item.Quantidade);

                    var orderItem = new OrderItem
                    {
                        ProdutoId = product.Id,
                        NomeProduto = product.Nome,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = precoUnitario
                    };

                    total += item.Quantidade * precoUnitario;

                    order.Items.Add(orderItem);
                }

                order.ValorTotal = total;
                order.ClientId = client.Id;
                ApplyEndereco(order, dto.EnderecoEntrega, client);

                _context.Orders.Add(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return order.Id;
            });

            // Reload with products for response
            var createdOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstAsync(o => o.Id == createdOrderId);

            return ToResponse(createdOrder, client.Nome);
        }

        public async Task<List<OrderResponseDTO>> GetAllAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Items)
                .ToListAsync();

            return orders.Select(o => ToResponse(o, o.Client.Nome)).ToList();
        }

        public async Task<OrderResponseDTO?> GetByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return null;

            return ToResponse(order, order.Client.Nome);
        }

        public async Task DeleteAsync(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                using var transaction = await _context.Database.BeginTransactionAsync();

                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    throw new KeyNotFoundException($"Order {id} not found.");

                // Restore stock (item.ProdutoId pode ser nulo se o produto
                // já foi excluído — nesse caso não tem o que repor).
                foreach (var item in order.Items)
                {
                    if (item.ProdutoId == null)
                        continue;

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
            });
        }

        public async Task<OrderResponseDTO> UpdateAsync(int id, CreateOrderDTO dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new ArgumentException("Order must contain at least one item.");

            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == dto.ClientId);

            if (client == null)
                throw new KeyNotFoundException($"Client {dto.ClientId} not found.");

            var strategy = _context.Database.CreateExecutionStrategy();

            var updatedOrderId = await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                using var transaction = await _context.Database.BeginTransactionAsync();

                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    throw new KeyNotFoundException($"Order {id} not found.");

                // Restore stock from existing items (ProdutoId pode ser
                // nulo se o produto já foi excluído).
                foreach (var existingItem in order.Items)
                {
                    if (existingItem.ProdutoId == null)
                        continue;

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

                    var precoUnitario = ResolvePrecoUnitario(product, item.Quantidade);

                    var orderItem = new OrderItem
                    {
                        ProdutoId = product.Id,
                        NomeProduto = product.Nome,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = precoUnitario
                    };

                    total += item.Quantidade * precoUnitario;

                    order.Items.Add(orderItem);
                }

                order.ValorTotal = total;
                order.ClientId = client.Id;
                ApplyEndereco(order, dto.EnderecoEntrega, client);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return order.Id;
            });

            var updatedOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstAsync(o => o.Id == updatedOrderId);

            return ToResponse(updatedOrder, client.Nome);
        }

        // Se o produto tem preço de atacado configurado e a quantidade do
        // item atinge o mínimo exigido, usa o preço de atacado; caso
        // contrário, usa o preço de varejo.
        private static decimal ResolvePrecoUnitario(Product product, int quantidade)
        {
            if (product.PrecoAtacado.HasValue &&
                product.QuantidadeMinimaAtacado.HasValue &&
                quantidade >= product.QuantidadeMinimaAtacado.Value)
            {
                return product.PrecoAtacado.Value;
            }

            return product.PrecoVarejo;
        }

        // Resolve o endereço de entrega do pedido: usa o informado no request
        // ou, na ausência dele, cai para o endereço cadastrado do cliente.
        private static void ApplyEndereco(Order order, EnderecoDTO? endereco, Client client)
        {
            if (endereco != null)
            {
                order.Cep = endereco.Cep;
                order.Rua = endereco.Rua;
                order.Bairro = endereco.Bairro;
                order.Cidade = endereco.Cidade;
                order.Estado = endereco.Estado;
                order.Numero = endereco.Numero;
                order.Complemento = endereco.Complemento ?? string.Empty;
                return;
            }

            if (string.IsNullOrWhiteSpace(client.Cep) ||
                string.IsNullOrWhiteSpace(client.Rua) ||
                string.IsNullOrWhiteSpace(client.Bairro) ||
                string.IsNullOrWhiteSpace(client.Cidade) ||
                string.IsNullOrWhiteSpace(client.Estado) ||
                string.IsNullOrWhiteSpace(client.Numero))
            {
                throw new ArgumentException(
                    "Client has no registered address; provide an EnderecoEntrega for this order.");
            }

            order.Cep = client.Cep;
            order.Rua = client.Rua;
            order.Bairro = client.Bairro;
            order.Cidade = client.Cidade;
            order.Estado = client.Estado;
            order.Numero = client.Numero;
            order.Complemento = client.Complemento ?? string.Empty;
        }

        public static OrderResponseDTO ToResponse(Order order, string nomeCliente)
        {
            return new OrderResponseDTO
            {
                Id = order.Id,
                CriadoEm = order.CriadoEm,
                ClientId = order.ClientId,
                NomeCliente = nomeCliente,
                Cep = order.Cep,
                Bairro = order.Bairro,
                Cidade = order.Cidade,
                Complemento = order.Complemento,
                Estado = order.Estado,
                Numero = order.Numero,
                Rua = order.Rua,
                ValorTotal = order.ValorTotal,
                Items = order.Items.Select(i => new OrderItemResponseDTO
                {
                    ProdutoId = i.ProdutoId,
                    NomeProduto = i.NomeProduto,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario
                }).ToList()
            };
        }
    }
}
