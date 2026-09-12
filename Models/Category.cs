namespace MuranoApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        // Ver Product.NomeNormalizado — mesmo propósito, índice único aqui.
        public string NomeNormalizado { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public List<Product> Products { get; set; } = new();
    }
}
