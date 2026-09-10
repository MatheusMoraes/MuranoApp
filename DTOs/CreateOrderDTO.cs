namespace MuranoApp.DTOs
{
    public class CreateOrderDTO
    {
        public int ClientId { get; set; }

        // Opcional: se não informado, usa o endereço cadastrado do cliente.
        public EnderecoDTO? EnderecoEntrega { get; set; }

        public List<OrderItemDTO> Items { get; set; } = new();
    }
}
