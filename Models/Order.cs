namespace MuranoApp.Models
{
    public class Order
    {   
        public int Id { get; set; }

        public string NomeCliente { get; set; }

        public string Cep { get; set; }
        public string Rua { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public decimal ValorTotal { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }

}
