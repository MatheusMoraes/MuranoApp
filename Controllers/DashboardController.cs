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

        // period: "30d" | "60d" | "90d" | "trimestre" | "semestre" | "ano".
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue([FromQuery] string period = "30d")
        {
            var service = new DashboardService(_context);

            try
            {
                var result = await service.GetRevenueByPeriodAsync(period);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
