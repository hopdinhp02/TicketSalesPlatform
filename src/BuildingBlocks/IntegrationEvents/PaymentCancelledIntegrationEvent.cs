namespace TicketSalesPlatform.IntegrationEvents
{
    public record PaymentCancelledIntegrationEvent(
        Guid PaymentId,
        Guid OrderId,
        string Reason,
        DateTime OccurredOn
    );
}
