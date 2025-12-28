namespace TicketSalesPlatform.Orders.Application.Clients
{
    public interface IInventoryClient
    {
        Task<bool> CheckStockAsync(
            Guid ticketTypeId,
            int quantity,
            CancellationToken cancellationToken = default
        );
    }
}
