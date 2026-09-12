using System.Text.RegularExpressions;

namespace MuranoApp.Services
{
    // Normalização de nome compartilhada entre Product e Category: usada
    // tanto pra comparar ("batata quente" == "BaTaTa   QUENTE") quanto pra
    // preencher a coluna NomeNormalizado, que tem índice único no banco —
    // assim a checagem de duplicidade vira uma busca indexada em vez de
    // carregar a tabela inteira pra memória a cada criação/edição.
    public static class NameNormalizer
    {
        public static string Normalize(string nome)
        {
            return Regex.Replace(nome.Trim(), @"\s+", " ").ToLowerInvariant();
        }
    }
}
