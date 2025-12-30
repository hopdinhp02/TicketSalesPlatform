namespace TicketSalesPlatform.IntegrationEvents
{
    public record OrderReservationFailedIntegrationEvent(Guid OrderId, string Reason);
}
