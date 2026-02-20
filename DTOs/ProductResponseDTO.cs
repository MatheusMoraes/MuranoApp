namespace MuranoApp.DTOs
{
    public class ProductResponseDTO
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public decimal Preco { get; set; }

        public int Quantidade { get; set; }

        public DateTime CriadoEm { get; set; }
    }
}
