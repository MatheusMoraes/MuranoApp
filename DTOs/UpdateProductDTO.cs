namespace MuranoApp.DTOs
{
    public class UpdateProductDTO
    {
        public string Nome { get; set; } = string.Empty;

        public int CategoriaId { get; set; }

        public decimal PrecoVarejo { get; set; }
        public decimal? PrecoAtacado { get; set; }
        public int? QuantidadeMinimaAtacado { get; set; }

        public int Quantidade { get; set; }
        public int? EstoqueMinimo { get; set; }

        public string? ImagemUrl { get; set; }
        public string? ImagemPublicId { get; set; }
    }
}
