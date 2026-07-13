namespace TicketSalesPlatform.Events.Domain.ReadModels
{
    public class TicketTypeView
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsPublished { get; set; }
    }
}
