namespace MuranoApp.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int ProdutoId { get; set; }
        public Product Produto { get; set; } = null!;

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int Quantidade { get; set; }

        public decimal PrecoUnitario { get; set; }
    }
}
