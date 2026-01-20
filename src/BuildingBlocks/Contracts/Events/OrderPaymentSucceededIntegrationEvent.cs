namespace TicketSalesPlatform.Contracts.Events
{
    public sealed record OrderPaymentSucceededIntegrationEvent(
        Guid OrderId,
        Guid PaymentId,
        DateTime PaidAt
    );
}
