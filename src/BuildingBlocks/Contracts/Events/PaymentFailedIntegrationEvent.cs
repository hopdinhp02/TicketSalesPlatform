namespace TicketSalesPlatform.Contracts.Events
{
    public sealed record PaymentFailedIntegrationEvent(
        Guid OrderId,
        Guid? PaymentId,
        string Reason,
        DateTime OccurredOn
    );
}
