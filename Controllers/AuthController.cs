using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MuranoApp.Data;
using MuranoApp.DTOs;
using MuranoApp.Services;
using System.Security.Claims;

namespace MuranoApp.Controllers
{
    // Rotas em inglês/PascalCase (Login/Logout/Me) direto sob /api para
    // casar com o que o front já espera (src/api/auth.ts).
    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private const string CookieName = "murano_auth";

        [HttpPost("Login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            var service = new AuthService(_context, _configuration);

            var result = await service.LoginAsync(dto);

            if (result == null)
                return Unauthorized(new { error = "Email ou senha inválidos." });

            Response.Cookies.Append(CookieName, result.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                // SameSite=None é necessário enquanto front (Vercel) e back
                // (Render) forem domínios diferentes; com o proxy reverso
                // ativo, o navegador só fala com o próprio domínio do front,
                // então o cookie continua funcionando normalmente.
                SameSite = SameSiteMode.None,
                // Sem "Lembrar", CookieExpiresAt vem null e não setamos
                // Expires: o cookie vira "de sessão" e some quando o
                // navegador fecha, mesmo o token em si durando algumas
                // horas. Com "Lembrar", persiste por 30 dias.
                Expires = result.CookieExpiresAt,
                Path = "/"
            });

            return Ok(result.User);
        }

        [HttpPost("Logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
            return Ok();
        }

        [HttpGet("Me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var service = new AuthService(_context, _configuration);

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (idClaim == null || !int.TryParse(idClaim, out var id))
                return Unauthorized();

            var user = await service.GetByIdAsync(id);
            if (user == null)
                return Unauthorized();

            return Ok(user);
        }
    }
}
