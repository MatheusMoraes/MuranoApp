namespace MuranoApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public decimal Preco { get; set; }

        public int Quantidade { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
