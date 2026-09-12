namespace MuranoApp.DTOs
{
    // Métricas simples pro dashboard: KPIs gerais + rankings (top clientes e
    // top produtos). Tudo calculado sob demanda a partir das tabelas de
    // pedido — sem tabela própria de agregação, o volume de dados de uma
    // loja pequena não justifica isso.
    public class DashboardResponseDTO
    {
        public int TotalPedidos { get; set; }
        public decimal ReceitaTotal { get; set; }
        public decimal TicketMedio { get; set; }
        public int TotalClientes { get; set; }
        public int TotalProdutos { get; set; }
        public int ProdutosComEstoqueBaixo { get; set; }

        public List<TopClienteDTO> TopClientes { get; set; } = new();
        public List<TopProdutoDTO> TopProdutos { get; set; } = new();
        public List<ReceitaPorCategoriaDTO> ReceitaPorCategoria { get; set; } = new();
    }

    public class TopClienteDTO
    {
        public int ClientId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int TotalPedidos { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class TopProdutoDTO
    {
        // Nulo se o produto já foi excluído — o ranking usa o nome
        // (NomeProduto, snapshot salvo em cada OrderItem) pra continuar
        // correto mesmo depois da exclusão do cadastro.
        public int? ProdutoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int QuantidadeVendida { get; set; }
        public decimal ReceitaGerada { get; set; }
    }

    public class ReceitaPorCategoriaDTO
    {
        public string CategoriaNome { get; set; } = string.Empty;
        public decimal ReceitaTotal { get; set; }
    }
}
