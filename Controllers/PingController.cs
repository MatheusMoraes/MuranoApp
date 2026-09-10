using Microsoft.AspNetCore.Mvc;
using MuranoApp.Data;

namespace MuranoApp.Controllers
{
    // Endpoint leve para o front chamar periodicamente (ex: a cada 10s) e
    // manter a conexão com o banco (Neon) ativa, evitando o cold start do
    // compute autosuspendido por inatividade.
    [ApiController]
    [Route("api/ping")]
    public class PingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var alive = await _context.Database.CanConnectAsync();

                if (!alive)
                    return StatusCode(503, new { alive = false });

                return Ok(new { alive = true, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { alive = false, error = ex.Message });
            }
        }
    }
}
