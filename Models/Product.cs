namespace MuranoApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public decimal PrecoVarejo { get; set; }

        // Preço e quantidade mínima para venda no atacado. Ambos nulos
        // significa que o produto não tem preço de atacado configurado.
        public decimal? PrecoAtacado { get; set; }
        public int? QuantidadeMinimaAtacado { get; set; }

        public int Quantidade { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
