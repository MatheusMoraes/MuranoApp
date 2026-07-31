using Microsoft.AspNetCore.Mvc;
using MuranoApp.DTOs.MelhorEnvioRequest;
using MuranoApp.Services;

namespace MuranoApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalculateShippingController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CalculateShipping(MelhorEnvioShippingCalculatorRequestDTO request)
        {
            var service = new CalculateShippingService();
            try
            {
                var result = await service.CalculateShippingAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
