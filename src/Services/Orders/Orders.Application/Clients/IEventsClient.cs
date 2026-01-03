namespace TicketSalesPlatform.Orders.Application.Clients
{
    public interface IEventsClient
    {
        Task<TicketTypeDto?> GetTicketTypeAsync(
            Guid ticketTypeId,
            CancellationToken cancellationToken = default
        );
    }

    public record TicketTypeDto(Guid Id, Guid EventId, string Name, decimal Price);
}
