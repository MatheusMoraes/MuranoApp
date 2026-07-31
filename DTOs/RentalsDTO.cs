namespace MuranoApp.DTOs
{
    public class RentalsDTO
    {
        public int Id { get; set; }
        public string CustomerCode { get; set; }
        public string Ticker { get; set; }
        public string ContractNumber { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime ContractStartDate { get; set; }
        public DateTime ContractEndDate { get; set; }
        public DateTime TradeDate { get; set; }
        public DateTime SettlementDate { get; set; }
        public string ContractType { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
