namespace TicketSalesPlatform.Contracts.Events
{
    public record PaymentCancelledIntegrationEvent(
        Guid PaymentId,
        Guid OrderId,
        string Reason,
        DateTime OccurredOn
    );
}
