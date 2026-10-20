namespace MuranoApp.Models
{
    // Usuário único de administração do painel. Não existe rota de
    // cadastro pública — este usuário é criado diretamente via migration
    // (seed). PasswordHash é gerado pelo PasswordHasher do ASP.NET Core
    // (PBKDF2 salgado); a senha em texto puro nunca é armazenada.
    public class AdminUser
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // Admin ("admin murano") vê e edita produtos/pedidos de qualquer
        // usuário; um usuário comum só vê os que ele mesmo cadastrou (ver
        // Product.UserId / Order.UserId e os Services correspondentes).
        public bool IsAdmin { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
