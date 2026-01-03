namespace TicketSalesPlatform.IntegrationEvents
{
    public record TicketTypeAddedIntegrationEvent(
        Guid EventId,
        Guid TicketTypeId,
        string Name,
        decimal Price,
        int Quantity
    );
}
