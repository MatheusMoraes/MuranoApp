using Microsoft.EntityFrameworkCore;
using MuranoApp.Data;
using MuranoApp.DTOs;

namespace MuranoApp.Services
{
    public class DashboardService
    {
        private const int TopN = 5;

        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardResponseDTO> GetAsync()
        {
            var totalPedidos = await _context.Orders.CountAsync();
            var receitaTotal = totalPedidos == 0
                ? 0m
                : await _context.Orders.SumAsync(o => o.ValorTotal);
            var ticketMedio = totalPedidos == 0 ? 0m : receitaTotal / totalPedidos;

            var totalClientes = await _context.Clients.CountAsync();
            var totalProdutos = await _context.Products.CountAsync();
            var produtosComEstoqueBaixo = await _context.Products
                .CountAsync(p => p.EstoqueMinimo != null && p.Quantidade <= p.EstoqueMinimo);

            // Top clientes por número de pedidos (desempate por valor total).
            // A ordenação final é feita em memória (depois do ToListAsync) em
            // vez de no banco: o SQLite (usado nos testes automatizados) não
            // traduz ORDER BY sobre coluna decimal, e como o volume de
            // clientes/pedidos de uma loja pequena é baixo, não há custo real
            // em ordenar client-side — o ganho é rodar igual em qualquer
            // provider.
            var topClientesRaw = await _context.Orders
                .GroupBy(o => new { o.ClientId, o.Client.Nome })
                .Select(g => new TopClienteDTO
                {
                    ClientId = g.Key.ClientId,
                    Nome = g.Key.Nome,
                    TotalPedidos = g.Count(),
                    ValorTotal = g.Sum(o => o.ValorTotal)
                })
                .ToListAsync();

            var topClientes = topClientesRaw
                .OrderByDescending(c => c.TotalPedidos)
                .ThenByDescending(c => c.ValorTotal)
                .Take(TopN)
                .ToList();

            // Top produtos por quantidade vendida. Agrupado pelo NomeProduto
            // (snapshot salvo em cada item) em vez do ProdutoId, pra somar
            // corretamente mesmo quando o produto já foi excluído do
            // cadastro — nesse caso ProdutoId fica nulo, mas o histórico de
            // vendas continua íntegro.
            var topProdutosRaw = await _context.OrderItems
                .GroupBy(i => i.NomeProduto)
                .Select(g => new TopProdutoDTO
                {
                    // Usa o ProdutoId mais recente do grupo só como referência
                    // (ex: link pro produto ainda existente); nulo se todos os
                    // itens do grupo já perderam a referência.
                    ProdutoId = g.OrderByDescending(i => i.Id).Select(i => i.ProdutoId).FirstOrDefault(),
                    Nome = g.Key,
                    QuantidadeVendida = g.Sum(i => i.Quantidade),
                    ReceitaGerada = g.Sum(i => i.Quantidade * i.PrecoUnitario)
                })
                .ToListAsync();

            var topProdutos = topProdutosRaw
                .OrderByDescending(p => p.QuantidadeVendida)
                .Take(TopN)
                .ToList();

            // Receita por categoria: melhor esforço — itens cujo produto já
            // foi excluído (ProdutoId nulo) não têm mais categoria conhecida
            // e ficam de fora dessa métrica específica (as outras acima
            // continuam corretas via NomeProduto).
            var receitaPorCategoriaRaw = await _context.OrderItems
                .Where(i => i.ProdutoId != null)
                .Select(i => new { i.Quantidade, i.PrecoUnitario, i.Produto!.Categoria.Nome })
                .GroupBy(x => x.Nome)
                .Select(g => new ReceitaPorCategoriaDTO
                {
                    CategoriaNome = g.Key,
                    ReceitaTotal = g.Sum(x => x.Quantidade * x.PrecoUnitario)
                })
                .ToListAsync();

            var receitaPorCategoria = receitaPorCategoriaRaw
                .OrderByDescending(c => c.ReceitaTotal)
                .ToList();

            return new DashboardResponseDTO
            {
                TotalPedidos = totalPedidos,
                ReceitaTotal = receitaTotal,
                TicketMedio = ticketMedio,
                TotalClientes = totalClientes,
                TotalProdutos = totalProdutos,
                ProdutosComEstoqueBaixo = produtosComEstoqueBaixo,
                TopClientes = topClientes,
                TopProdutos = topProdutos,
                ReceitaPorCategoria = receitaPorCategoria
            };
        }
    }
}
