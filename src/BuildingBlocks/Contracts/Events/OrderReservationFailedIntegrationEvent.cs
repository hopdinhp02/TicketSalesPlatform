namespace TicketSalesPlatform.Contracts.Events
{
    public record OrderReservationFailedIntegrationEvent(Guid OrderId, string Reason);
}
