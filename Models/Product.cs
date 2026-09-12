namespace MuranoApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        // Nome normalizado (trim + espaços colapsados + minúsculo) usado
        // pra checar duplicidade via índice único no banco, em vez de
        // carregar a tabela inteira pra memória a cada criação/edição.
        // Ver NameNormalizer.
        public string NomeNormalizado { get; set; } = string.Empty;

        // Todo produto pertence a uma categoria — usado pra filtro/busca na
        // tela de produtos.
        public int CategoriaId { get; set; }
        public Category Categoria { get; set; } = null!;

        public decimal PrecoVarejo { get; set; }

        // Preço e quantidade mínima para venda no atacado. Ambos nulos
        // significa que o produto não tem preço de atacado configurado.
        public decimal? PrecoAtacado { get; set; }
        public int? QuantidadeMinimaAtacado { get; set; }

        public int Quantidade { get; set; }

        // Limite pra considerar o produto com estoque baixo. Nulo = alerta
        // desativado pra esse produto.
        public int? EstoqueMinimo { get; set; }

        // Guardamos só a referência (URL) — os bytes da imagem ficam na
        // Cloudinary, não no Postgres. ImagemPublicId é o identificador da
        // Cloudinary pro arquivo, usado pra conseguir apagar/trocar a
        // imagem depois sem deixar lixo órfão na conta.
        public string? ImagemUrl { get; set; }
        public string? ImagemPublicId { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
