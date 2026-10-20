namespace MuranoApp.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;

        // Endereço de entrega resolvido no momento da criação do pedido
        // (copiado do cadastro do cliente, ou informado à parte para este pedido).
        public string Cep { get; set; }
        public string Rua { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public decimal ValorTotal { get; set; }

        // Dono do cadastro (quem criou o pedido) — usuários comuns só veem/
        // editam os próprios pedidos; o admin vê todos, ignorando este
        // campo. Ver OrderService.
        public int UserId { get; set; }
        public AdminUser User { get; set; } = null!;

        public List<OrderItem> Items { get; set; } = new();
    }

}
