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

            // Produtos mais caros vendidos: entre os que já venderam pelo
            // menos 1 unidade, os 5 de maior preço de varejo atual —
            // diferente do ranking por quantidade vendida acima, esse mostra
            // se os itens premium do catálogo também estão girando. Só
            // produtos que ainda existem no cadastro (preço atual
            // disponível); os excluídos ficam de fora dessa métrica
            // específica, igual à receita por categoria.
            var produtosMaisCarosRaw = await _context.OrderItems
                .Where(i => i.ProdutoId != null)
                .GroupBy(i => new { ProdutoId = i.ProdutoId!.Value, i.Produto!.Nome, i.Produto.PrecoVarejo })
                .Select(g => new TopProdutoCaroDTO
                {
                    ProdutoId = g.Key.ProdutoId,
                    Nome = g.Key.Nome,
                    PrecoVarejo = g.Key.PrecoVarejo,
                    QuantidadeVendida = g.Sum(i => i.Quantidade)
                })
                .ToListAsync();

            var produtosMaisCarosVendidos = produtosMaisCarosRaw
                .OrderByDescending(p => p.PrecoVarejo)
                .ThenByDescending(p => p.QuantidadeVendida)
                .Take(TopN)
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
                ReceitaPorCategoria = receitaPorCategoria,
                ProdutosMaisCarosVendidos = produtosMaisCarosVendidos
            };
        }

        // Períodos aceitos pelo card "Receita por período" do dashboard, e a
        // granularidade de agrupamento de cada um — quanto maior o período,
        // mais grossa a granularidade, pra manter o gráfico legível (um ano
        // em pontos diários teria 365 pontos; em pontos mensais, ~12).
        private static readonly Dictionary<string, (int? dias, int? meses, string granularidade)> Periodos = new()
        {
            ["30d"] = (30, null, "dia"),
            ["60d"] = (60, null, "dia"),
            ["90d"] = (90, null, "dia"),
            ["trimestre"] = (null, 3, "semana"),
            ["semestre"] = (null, 6, "semana"),
            ["ano"] = (null, 12, "mes"),
        };

        public async Task<RevenueByPeriodResponseDTO> GetRevenueByPeriodAsync(string periodo)
        {
            if (!Periodos.TryGetValue(periodo, out var config))
                throw new ArgumentException(
                    $"Período inválido: \"{periodo}\". Use um de: {string.Join(", ", Periodos.Keys)}.");

            var hoje = DateTime.UtcNow.Date;
            var dataFimExclusiva = hoje.AddDays(1);
            var dataInicio = config.dias.HasValue
                ? hoje.AddDays(-(config.dias.Value - 1))
                : hoje.AddMonths(-config.meses!.Value);

            // Busca tudo no intervalo de uma vez e agrupa em memória — o
            // volume de pedidos de uma loja pequena, mesmo num ano inteiro,
            // é baixo o bastante pra isso não pesar, e evita depender de
            // tradução de agrupamento por data específica de cada provider
            // (Postgres em produção, SQLite nos testes automatizados).
            var orders = await _context.Orders
                .Where(o => o.CriadoEm >= dataInicio && o.CriadoEm < dataFimExclusiva)
                .Select(o => new OrderRevenueRow(o.CriadoEm, o.ValorTotal))
                .ToListAsync();

            var pontos = config.granularidade switch
            {
                "semana" => BucketPorSemana(orders, dataInicio, dataFimExclusiva),
                "mes" => BucketPorMes(orders, dataInicio, dataFimExclusiva),
                _ => BucketPorDia(orders, dataInicio, dataFimExclusiva)
            };

            var receitaTotal = orders.Sum(o => o.ValorTotal);
            var totalPedidos = orders.Count;
            var ticketMedio = totalPedidos == 0 ? 0m : receitaTotal / totalPedidos;

            // Período anterior: mesma duração, terminando exatamente onde o
            // período atual começa — dá pra comparar "últimos 30 dias" com
            // os 30 dias antes deles, sem se importar se a duração veio de
            // dias ou de meses (trimestre/semestre/ano).
            var duracao = dataFimExclusiva - dataInicio;
            var dataInicioAnterior = dataInicio - duracao;
            var receitaAnterior = await _context.Orders
                .Where(o => o.CriadoEm >= dataInicioAnterior && o.CriadoEm < dataInicio)
                .SumAsync(o => (decimal?)o.ValorTotal) ?? 0m;

            decimal? variacaoPercentual = receitaAnterior > 0
                ? Math.Round((receitaTotal - receitaAnterior) / receitaAnterior * 100, 1)
                : null;

            return new RevenueByPeriodResponseDTO
            {
                Periodo = periodo,
                DataInicio = dataInicio,
                DataFim = hoje,
                Granularidade = config.granularidade,
                ReceitaTotal = receitaTotal,
                TotalPedidos = totalPedidos,
                TicketMedio = ticketMedio,
                ReceitaPeriodoAnterior = receitaAnterior,
                VariacaoPercentual = variacaoPercentual,
                Pontos = pontos
            };
        }

        // Um ponto por dia — inclui dias sem pedido (receita 0) pra manter o
        // eixo X contínuo em vez de pular datas sem venda.
        private static List<RevenuePointDTO> BucketPorDia(List<OrderRevenueRow> orders, DateTime inicio, DateTime fimExclusiva)
        {
            var porDia = orders.ToLookup(o => o.CriadoEm.Date);

            var pontos = new List<RevenuePointDTO>();
            for (var dia = inicio; dia < fimExclusiva; dia = dia.AddDays(1))
            {
                pontos.Add(new RevenuePointDTO { Data = dia, Receita = porDia[dia].Sum(o => o.ValorTotal) });
            }
            return pontos;
        }

        // Janelas de 7 dias a partir do início do período (não é semana ISO
        // — é só uma janela rolante a partir da data de início, mais simples
        // e suficiente pra um gráfico de tendência).
        private static List<RevenuePointDTO> BucketPorSemana(List<OrderRevenueRow> orders, DateTime inicio, DateTime fimExclusiva)
        {
            var pontos = new List<RevenuePointDTO>();

            for (var inicioSemana = inicio; inicioSemana < fimExclusiva; inicioSemana = inicioSemana.AddDays(7))
            {
                var fimSemanaExclusiva = inicioSemana.AddDays(7);
                if (fimSemanaExclusiva > fimExclusiva) fimSemanaExclusiva = fimExclusiva;

                var receita = orders
                    .Where(o => o.CriadoEm >= inicioSemana && o.CriadoEm < fimSemanaExclusiva)
                    .Sum(o => o.ValorTotal);

                pontos.Add(new RevenuePointDTO { Data = inicioSemana, Receita = receita });
            }
            return pontos;
        }

        // Um ponto por mês calendário, respeitando janelas parciais nas
        // pontas do período (ex: período começa dia 15 — o primeiro ponto
        // soma só do dia 15 em diante daquele mês).
        private static List<RevenuePointDTO> BucketPorMes(List<OrderRevenueRow> orders, DateTime inicio, DateTime fimExclusiva)
        {
            var pontos = new List<RevenuePointDTO>();

            var inicioMes = new DateTime(inicio.Year, inicio.Month, 1);
            while (inicioMes < fimExclusiva)
            {
                var proximoMes = inicioMes.AddMonths(1);
                var janelaInicio = inicioMes > inicio ? inicioMes : inicio;
                var janelaFim = proximoMes < fimExclusiva ? proximoMes : fimExclusiva;

                var receita = orders
                    .Where(o => o.CriadoEm >= janelaInicio && o.CriadoEm < janelaFim)
                    .Sum(o => o.ValorTotal);

                pontos.Add(new RevenuePointDTO { Data = inicioMes, Receita = receita });
                inicioMes = proximoMes;
            }
            return pontos;
        }

        private readonly record struct OrderRevenueRow(DateTime CriadoEm, decimal ValorTotal);
    }
}
