namespace MuranoApp.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        // Nulo quando o produto foi excluído depois da compra — o pedido
        // continua existindo normalmente, só perde a referência viva.
        public int? ProdutoId { get; set; }
        public Product? Produto { get; set; }

        // Nome do produto no momento da compra. Não depende do produto
        // ainda existir (mesmo princípio do PrecoUnitario abaixo: o pedido é
        // um registro histórico, não deve mudar se o cadastro mudar depois).
        public string NomeProduto { get; set; } = string.Empty;

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int Quantidade { get; set; }

        public decimal PrecoUnitario { get; set; }
    }
}
