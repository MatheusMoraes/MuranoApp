namespace MuranoApp.DTOs
{
    public class CategoryResponseDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
