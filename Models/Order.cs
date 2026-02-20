namespace MuranoApp.Models
{
    public class Order
    {
        public int Id { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public decimal ValorTotal { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}
