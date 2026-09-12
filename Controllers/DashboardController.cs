using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuranoApp.Data;
using MuranoApp.Services;

namespace MuranoApp.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var service = new DashboardService(_context);
            var result = await service.GetAsync();
            return Ok(result);
        }
    }
}
