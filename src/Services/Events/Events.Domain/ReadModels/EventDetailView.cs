namespace TicketSalesPlatform.Events.Domain.ReadModels
{
    public class EventDetailView
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public List<TicketTypeDto> TicketTypes { get; set; } = new();
    }

    public record TicketTypeDto(Guid Id, Guid EventId, string Name, decimal Price, int Quantity);
}
