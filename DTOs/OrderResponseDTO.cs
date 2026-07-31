namespace MuranoApp.DTOs
{
    public class OrderResponseDTO
    {
        public int Id { get; set; }

        public DateTime CriadoEm { get; set; }
        public string NomeCliente { get; set; }
        public string Cep { get; set; }
        public string Rua { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public decimal ValorTotal { get; set; }

        public List<OrderItemResponseDTO> Items { get; set; } = new();
    }
}
