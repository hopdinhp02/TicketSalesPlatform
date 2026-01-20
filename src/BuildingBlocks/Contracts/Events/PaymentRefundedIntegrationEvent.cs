namespace TicketSalesPlatform.Contracts.Events
{
    public record PaymentRefundedIntegrationEvent(
        Guid PaymentId,
        Guid OrderId,
        DateTime OccurredOn
    );
}
