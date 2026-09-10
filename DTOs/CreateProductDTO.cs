namespace MuranoApp.DTOs
{
    public class CreateProductDTO
    {
        public string Nome { get; set; } = string.Empty;

        public decimal PrecoVarejo { get; set; }
        public decimal? PrecoAtacado { get; set; }
        public int? QuantidadeMinimaAtacado { get; set; }

        public int Quantidade { get; set; }
        public int? EstoqueMinimo { get; set; }
    }
}
