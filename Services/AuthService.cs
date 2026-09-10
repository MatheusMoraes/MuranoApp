using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MuranoApp.Data;
using MuranoApp.DTOs;
using MuranoApp.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MuranoApp.Services
{
    public class LoginResult
    {
        public string Token { get; set; } = string.Empty;
        public UserResponseDTO User { get; set; } = null!;

        // Se null, o controller trata o cookie como "de sessão" (some
        // quando o navegador fecha) — usado quando "Lembrar" não foi
        // marcado no login.
        public DateTimeOffset? CookieExpiresAt { get; set; }
    }

    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<AdminUser> _hasher = new();

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginResult?> LoginAsync(LoginDTO dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.AdminUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
                return null;

            var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Senha);

            if (verification == PasswordVerificationResult.Failed)
                return null;

            var jwtSection = _configuration.GetSection("Jwt");
            var expiryHours = double.Parse(dto.Lembrar
                ? jwtSection["ExpiryHoursRemember"] ?? "720"  // 30 dias
                : jwtSection["ExpiryHours"] ?? "8");
            var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

            return new LoginResult
            {
                Token = GenerateToken(user, expiresAt),
                User = ToResponse(user),
                CookieExpiresAt = dto.Lembrar ? expiresAt : null
            };
        }

        public async Task<UserResponseDTO?> GetByIdAsync(int id)
        {
            var user = await _context.AdminUsers.FindAsync(id);
            return user == null ? null : ToResponse(user);
        }

        private string GenerateToken(AdminUser user, DateTime expiresAtUtc)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Nome),
            };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static UserResponseDTO ToResponse(AdminUser user)
        {
            return new UserResponseDTO
            {
                Id = user.Id,
                Nome = user.Nome,
                Email = user.Email
            };
        }
    }
}
