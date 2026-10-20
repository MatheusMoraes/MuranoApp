using System.Security.Claims;

namespace MuranoApp.Extensions
{
    // Leitura do usuário autenticado a partir das claims do JWT — usado por
    // todo controller que precisa saber "quem" está fazendo a chamada pra
    // filtrar por dono do cadastro (Product/Order) ou liberar acesso total
    // pro admin. Só deve ser chamado em rotas com [Authorize] (GetUserId
    // lança se a claim não existir, o que não deveria acontecer ali).
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (idClaim == null || !int.TryParse(idClaim, out var id))
                throw new InvalidOperationException("Usuário autenticado sem claim de id válida.");

            return id;
        }

        public static bool IsAdminUser(this ClaimsPrincipal user)
        {
            return user.IsInRole("Admin");
        }
    }
}
