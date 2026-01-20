namespace TicketSalesPlatform.Contracts.Events
{
    public sealed record StockReservedIntegrationEvent(Guid OrderId, DateTime OccurredOn);
}
