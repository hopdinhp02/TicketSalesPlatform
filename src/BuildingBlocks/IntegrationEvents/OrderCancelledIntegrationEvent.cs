namespace TicketSalesPlatform.IntegrationEvents
{
    public record OrderCancelledIntegrationEvent(Guid OrderId, string Reason);
}
