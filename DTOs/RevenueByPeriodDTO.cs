namespace MuranoApp.DTOs
{
    // Um ponto da série temporal de receita — Data é o início do bucket
    // (o próprio dia, o início da semana ou o início do mês, dependendo de
    // Granularidade em RevenueByPeriodResponseDTO).
    public class RevenuePointDTO
    {
        public DateTime Data { get; set; }
        public decimal Receita { get; set; }
    }

    public class RevenueByPeriodResponseDTO
    {
        // Eco do período pedido (ex: "30d", "trimestre") — útil pro front
        // confirmar que a resposta corresponde ao filtro selecionado.
        public string Periodo { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        // "dia" | "semana" | "mes" — quanto maior o período, mais grossa a
        // granularidade, pra manter o gráfico legível (ex: um ano em
        // pontos diários teria 365 pontos).
        public string Granularidade { get; set; } = string.Empty;

        public decimal ReceitaTotal { get; set; }
        public int TotalPedidos { get; set; }
        public decimal TicketMedio { get; set; }

        // Comparação com o período imediatamente anterior, de mesma
        // duração (ex: nos últimos 30 dias, compara com os 30 dias antes
        // desses). VariacaoPercentual fica nula quando não há receita no
        // período anterior pra comparar (divisão por zero não faz sentido
        // como percentual).
        public decimal ReceitaPeriodoAnterior { get; set; }
        public decimal? VariacaoPercentual { get; set; }

        public List<RevenuePointDTO> Pontos { get; set; } = new();
    }
}
