using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuranoApp.Data;
using MuranoApp.DTOs;
using MuranoApp.Extensions;
using MuranoApp.Services;

namespace MuranoApp.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public ProductsController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDTO dto)
        {
            var service = new ProductService(_context);

            try
            {
                var result = await service.CreateAsync(dto, User.GetUserId());

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Id },
                    result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var service = new ProductService(_context);

            var result = await service.GetByIdAsync(id, User.GetUserId(), User.IsAdminUser());

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var service = new ProductService(_context);

            var result = await service.GetAllAsync(User.GetUserId(), User.IsAdminUser());

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateProductDTO dto)
        {
            var service = new ProductService(_context, new CloudinaryService(_configuration));

            try
            {
                var updated = await service.UpdateAsync(id, dto, User.GetUserId(), User.IsAdminUser());

                if (!updated)
                    return NotFound();

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var service = new ProductService(_context, new CloudinaryService(_configuration));

            var deleted = await service.DeleteAsync(id, User.GetUserId(), User.IsAdminUser());

            if (!deleted)
                return NotFound();

            return NoContent();
        }

        // Upload fica separado do Create/Update: o front sobe a imagem
        // primeiro, recebe a URL/publicId de volta, e só então inclui isso
        // no payload de criar/editar o produto.
        [HttpPost("upload-image")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var cloudinaryService = new CloudinaryService(_configuration);

            try
            {
                var result = await cloudinaryService.UploadProductImageAsync(file);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
