namespace MuranoApp.DTOs
{
    public class OrderResponseDTO
    {
        public int Id { get; set; }

        public DateTime CriadoEm { get; set; }

        public decimal ValorTotal { get; set; }

        public List<OrderItemResponseDTO> Items { get; set; } = new();
    }
}
