namespace TicketSalesPlatform.Orders.Domain.ReadModels
{
    public class OrderDetails
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public List<OrderItemDetails> Items { get; set; } = new();
        public DateTime PlacedOn { get; set; }
        public string? CancellationReason { get; set; }
    }

    public class OrderItemDetails
    {
        public Guid TicketTypeId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}
