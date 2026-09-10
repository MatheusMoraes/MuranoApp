using Microsoft.EntityFrameworkCore;
using MuranoApp.Data;
using MuranoApp.DTOs;
using MuranoApp.Models;

namespace MuranoApp.Services
{
    public class ClientService
    {
        private readonly AppDbContext _context;

        public ClientService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ClientResponseDTO> CreateAsync(CreateClientDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new ArgumentException("Name is required.");

            var client = new Client
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Telefone = dto.Telefone,
                Cep = dto.Cep,
                Rua = dto.Rua,
                Bairro = dto.Bairro,
                Cidade = dto.Cidade,
                Estado = dto.Estado,
                Numero = dto.Numero,
                Complemento = dto.Complemento
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return ToResponse(client);
        }

        public async Task<ClientResponseDTO?> GetByIdAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
                return null;

            return ToResponse(client);
        }

        public async Task<List<ClientResponseDTO>> GetAllAsync()
        {
            var clients = await _context.Clients.ToListAsync();

            return clients.Select(ToResponse).ToList();
        }

        public async Task<ClientWithOrdersResponseDTO?> GetWithOrdersAsync(int id)
        {
            var client = await _context.Clients
                .Include(c => c.Orders)
                .ThenInclude(o => o.Items)
                .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
                return null;

            return new ClientWithOrdersResponseDTO
            {
                Id = client.Id,
                Nome = client.Nome,
                Email = client.Email,
                Telefone = client.Telefone,
                Cep = client.Cep,
                Rua = client.Rua,
                Bairro = client.Bairro,
                Cidade = client.Cidade,
                Estado = client.Estado,
                Numero = client.Numero,
                Complemento = client.Complemento,
                CriadoEm = client.CriadoEm,
                Orders = client.Orders
                    .OrderByDescending(o => o.CriadoEm)
                    .Select(o => OrderService.ToResponse(o, client.Nome))
                    .ToList()
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateClientDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new ArgumentException("Name is required.");

            var client = await _context.Clients.FindAsync(id);

            if (client == null)
                return false;

            client.Nome = dto.Nome;
            client.Email = dto.Email;
            client.Telefone = dto.Telefone;
            client.Cep = dto.Cep;
            client.Rua = dto.Rua;
            client.Bairro = dto.Bairro;
            client.Cidade = dto.Cidade;
            client.Estado = dto.Estado;
            client.Numero = dto.Numero;
            client.Complemento = dto.Complemento;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var client = await _context.Clients
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
                return false;

            if (client.Orders.Count > 0)
                throw new InvalidOperationException(
                    "Cannot delete a client that has orders.");

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return true;
        }

        private ClientResponseDTO ToResponse(Client client)
        {
            return new ClientResponseDTO
            {
                Id = client.Id,
                Nome = client.Nome,
                Email = client.Email,
                Telefone = client.Telefone,
                Cep = client.Cep,
                Rua = client.Rua,
                Bairro = client.Bairro,
                Cidade = client.Cidade,
                Estado = client.Estado,
                Numero = client.Numero,
                Complemento = client.Complemento,
                CriadoEm = client.CriadoEm
            };
        }
    }
}
