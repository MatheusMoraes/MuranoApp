namespace MuranoApp.DTOs
{
    public class CreateProductDTO
    {
        public string Nome { get; set; } = string.Empty;

        public decimal Preco { get; set; }

        public int Quantidade { get; set; }
    }
}
