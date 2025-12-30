namespace TicketSalesPlatform.IntegrationEvents
{
    public record PaymentRefundedIntegrationEvent(
        Guid PaymentId,
        Guid OrderId,
        DateTime OccurredOn
    );
}
