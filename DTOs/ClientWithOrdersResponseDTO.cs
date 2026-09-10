namespace MuranoApp.DTOs
{
    public class ClientWithOrdersResponseDTO
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? Telefone { get; set; }

        public string? Cep { get; set; }
        public string? Rua { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }

        public DateTime CriadoEm { get; set; }

        public List<OrderResponseDTO> Orders { get; set; } = new();
    }
}
