namespace TicketFlow.IntegrationEvents
{
    public sealed record OrderPlacedIntegrationEvent(
        Guid OrderId,
        Guid CustomerId,
        decimal TotalPrice,
        DateTime OrderDate
    );
}
