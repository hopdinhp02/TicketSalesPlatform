namespace TicketSalesPlatform.Contracts.Events
{
    public record RefundRequiredIntegrationEvent(Guid OrderId, string Reason);
}
