namespace TicketSalesPlatform.Contracts.Events
{
    public record OrderCompletedIntegrationEvent(
        Guid OrderId,
        Guid CustomerId,
        decimal TotalPrice,
        DateTime CompletedAt
    );
}
