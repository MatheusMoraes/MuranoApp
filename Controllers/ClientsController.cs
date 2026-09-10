using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuranoApp.Data;
using MuranoApp.DTOs;
using MuranoApp.Services;

namespace MuranoApp.Controllers
{
    [ApiController]
    [Route("api/clients")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateClientDTO dto)
        {
            var service = new ClientService(_context);

            try
            {
                var result = await service.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Id },
                    result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var service = new ClientService(_context);

            var result = await service.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var service = new ClientService(_context);

            var result = await service.GetAllAsync();

            return Ok(result);
        }

        // Cliente + histórico de pedidos, para a aba de detalhes do cliente.
        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var service = new ClientService(_context);

            var result = await service.GetWithOrdersAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateClientDTO dto)
        {
            var service = new ClientService(_context);

            try
            {
                var updated = await service.UpdateAsync(id, dto);

                if (!updated)
                    return NotFound();

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var service = new ClientService(_context);

            try
            {
                var deleted = await service.DeleteAsync(id);

                if (!deleted)
                    return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
