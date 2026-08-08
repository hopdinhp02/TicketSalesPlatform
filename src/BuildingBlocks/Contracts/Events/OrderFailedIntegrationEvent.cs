namespace TicketSalesPlatform.Contracts.Events
{
    public record OrderFailedIntegrationEvent(
        Guid OrderId,
        Guid CustomerId,
        string Reason,
        DateTime FailedAt
    );
}
