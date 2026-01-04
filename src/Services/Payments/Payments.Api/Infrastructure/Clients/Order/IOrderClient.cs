namespace TicketSalesPlatform.Payments.Api.Infrastructure.Clients.Order
{
    public interface IOrderClient
    {
        Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    }

    public record OrderDto(Guid Id, Guid CustomerId, decimal TotalPrice, string Status);
}
