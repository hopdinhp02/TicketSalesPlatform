namespace TicketSalesPlatform.IntegrationEvents
{
    public sealed record OrderPaymentSucceededIntegrationEvent(
        Guid OrderId,
        Guid PaymentId,
        DateTime PaidAt
    );
}
