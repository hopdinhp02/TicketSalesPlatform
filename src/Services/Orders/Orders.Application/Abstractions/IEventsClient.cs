namespace TicketFlow.Orders.Application.Abstractions
{
    public interface IEventsClient
    {
        Task<TicketAvailabilityDto?> GetTicketAvailabilityAsync(
            Guid eventId,
            Guid ticketTypeId,
            CancellationToken cancellationToken = default
        );
    }

    public record TicketAvailabilityDto(Guid TicketTypeId, int AvailableQuantity);
}
