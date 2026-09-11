namespace MuranoApp.DTOs
{
    public class ProductResponseDTO
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public int CategoriaId { get; set; }
        public string CategoriaNome { get; set; } = string.Empty;

        public decimal PrecoVarejo { get; set; }
        public decimal? PrecoAtacado { get; set; }
        public int? QuantidadeMinimaAtacado { get; set; }

        public int Quantidade { get; set; }
        public int? EstoqueMinimo { get; set; }
        // Calculado no backend: true quando EstoqueMinimo está configurado
        // e a quantidade em estoque já chegou nesse limite.
        public bool EstoqueBaixo { get; set; }

        public string? ImagemUrl { get; set; }
        // Exposto pro front conseguir reenviar sem mudança em updates que
        // não mexem na imagem — se não vier de volta, o backend interpreta
        // como "imagem removida" e apaga a antiga na Cloudinary.
        public string? ImagemPublicId { get; set; }

        public DateTime CriadoEm { get; set; }
    }
}
