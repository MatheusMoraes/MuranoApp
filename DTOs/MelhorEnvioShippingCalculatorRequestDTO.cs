namespace MuranoApp.DTOs.MelhorEnvioRequest
{
    public class MelhorEnvioShippingCalculatorRequestDTO
    {
        public From from { get; set; }
        public To to { get; set; }
        public Product[] products { get; set; }
        public Options options { get; set; }
        public string services { get; set; }
    }

    public class From
    {
        public string postal_code { get; set; }
    }

    public class To
    {
        public string postal_code { get; set; }
    }

    public class Options
    {
        public bool receipt { get; set; }
        public bool own_hand { get; set; }
    }

    public class Product
    {
        public string id { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int length { get; set; }
        public float weight { get; set; }
        public float insurance_value { get; set; }
        public int quantity { get; set; }
    }
}
