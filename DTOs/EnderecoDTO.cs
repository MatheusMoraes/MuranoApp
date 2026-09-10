namespace MuranoApp.DTOs
{
    // Endereço de entrega específico de um pedido. Quando não informado na
    // criação/atualização do pedido, usa-se o endereço cadastrado do cliente.
    public class EnderecoDTO
    {
        public string Cep { get; set; } = string.Empty;
        public string Rua { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string? Complemento { get; set; }
    }
}
