namespace TicketSalesPlatform.Events.Domain.ReadModels
{
    /// <summary>
    /// A denormalized, read-optimized model representing an event summary.
    /// This will be stored as a JSON document in the database.
    /// </summary>
    public class EventSummary
    {
        // Marten requires a public Id property for its documents.
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool IsPublished { get; set; }
        public int TotalTickets { get; set; }
        public decimal MinPrice { get; set; } = decimal.MaxValue;
        public decimal MaxPrice { get; set; } = 0;
    }
}
