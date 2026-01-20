namespace TicketSalesPlatform.Contracts.Events
{
    public record OrderCancelledIntegrationEvent(Guid OrderId, string Reason);
}
