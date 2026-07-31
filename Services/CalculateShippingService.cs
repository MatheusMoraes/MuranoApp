using MuranoApp.DTOs.MelhorEnvioRequest;
using MuranoApp.DTOs.MelhorEnvioResponse.MuranoApp.DTOs.MelhorEnvioResponse;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MuranoApp.Services
{
    public class CalculateShippingService
    {
        public async Task<List<MelhorEnvioShippingCalculatorResponseDTO>?> CalculateShippingAsync(MelhorEnvioShippingCalculatorRequestDTO request)
        {
            using var httpClient = new HttpClient();

            // Base da API
            httpClient.BaseAddress = new Uri("https://melhorenvio.com.br");

            // Timeout opcional
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Header padrão JSON
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // Header de autenticação Bearer
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJhdWQiOiIxIiwianRpIjoiYjBhMDZiYWI4MDFkN2QxZmEwOTQ2MTE3M2MxMzEzZmViMGI2ZWFmYjk4NzIwYWJhMTlmMWZhMWI5NDc3NDI0ODAxMTAzNmI3NTE5NzE5ODQiLCJpYXQiOjE3NzE5ODMxNzIuNjA5NjcsIm5iZiI6MTc3MTk4MzE3Mi42MDk2NzIsImV4cCI6MTgwMzUxOTE3Mi41OTk4NDIsInN1YiI6ImExMjkyZTgyLWY0MjEtNDJhZS05YTIzLWNmMTFkMWJiMWVkNCIsInNjb3BlcyI6WyJzaGlwcGluZy1jYWxjdWxhdGUiLCJzaGlwcGluZy10cmFja2luZyJdfQ.3gbDjzh457h5zbOhLRr3pcbZui-TvBjV80dKeyT5eVPBPx04EjG4x9m_h0AvMbITCo88JHG6jcvdnxKmcewrkz7kzNd9EhtLZk0VOuJrEFVHcevyq28vkvgpRUHuLb2Yme0WJHIULYgJEmn626Junuky531FLlcsDWKSMhhp8G5Y_hQSmyYp2tVv4tDc2Tf5CjlhC8CxBEkOlT31oXKL7dF7pBiTHnz9-sAC_YUA4-VOffYFYM40b53juddbU4dIu9fGjRyWR_XTEgnM8I3ZxZxWs2aj04dwA8UY8qh5GzZSDGU8LUCA1r660xOd7gMj6MK1o1HwpDzBlQUu5tWxwVYxeiUFBq-pENIF83J3trxko03J9vY7su_gZVhaMRr-tXGy46HeXTCCA5QqTQ1DNRlmpCnc2LjQ51Uwvxj3xPAfwnMcvkzmPCkE_ug2U6-GNEozd6zSa47NfISOr4-ZWR-amqgXo5IEjJ9mhpN_6rgAqjOsqnT2FvcP_5BChYU9g2mEF1-FFmAMRdSOKrAa-MJSXi46p2Gq2NPteanGJx0xmMrUmUssxcA7gr-hR8BMfHmUmfQJ4EWN41C3e3Ih9v65FWeGT1wFddX_f4iZzZJsiSjUhnbz3S6Bq0Nn4HEC4z6vZQ6i5N8T9Ph2xHFik02yD4eKMCuHJ5PIm-Qj2CA");

            // Serializando o payload
            var json = JsonSerializer.Serialize(request);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            // Chamada POST
            var response = await httpClient.PostAsync("/api/v2/me/shipment/calculate", content);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Erro na API: {response.StatusCode} - {responseContent}");
            }

            // Desserializando resposta
            return JsonSerializer.Deserialize<List<MelhorEnvioShippingCalculatorResponseDTO>>(
                responseContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
    }
}
