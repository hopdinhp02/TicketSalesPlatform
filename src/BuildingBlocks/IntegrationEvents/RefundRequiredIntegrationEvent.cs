namespace TicketSalesPlatform.IntegrationEvents
{
    public record RefundRequiredIntegrationEvent(Guid OrderId, string Reason);
}
