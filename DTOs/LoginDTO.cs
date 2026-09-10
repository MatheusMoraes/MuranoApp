namespace MuranoApp.DTOs
{
    public class LoginDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;

        // "Lembrar de mim": sessão de 30 dias em vez de expirar no fim da
        // sessão do navegador.
        public bool Lembrar { get; set; }
    }
}
