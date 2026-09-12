using MuranoApp.Services;
using Xunit;

namespace MuranoApp.Tests
{
    public class NameNormalizerTests
    {
        [Theory]
        [InlineData("batata quente", "batata quente")]
        [InlineData("BaTaTa   QUENTE", "batata quente")]
        [InlineData("  Batata Quente  ", "batata quente")]
        [InlineData("Batata\tQuente", "batata quente")]
        public void Normalize_IgnoraCaixaEEspacos(string entrada, string esperado)
        {
            Assert.Equal(esperado, NameNormalizer.Normalize(entrada));
        }
    }
}
