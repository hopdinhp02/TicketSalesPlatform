namespace TicketSalesPlatform.Contracts.Events
{
    public record TicketTypeAddedIntegrationEvent(
        Guid EventId,
        Guid TicketTypeId,
        string Name,
        decimal Price,
        int Quantity
    );
}
