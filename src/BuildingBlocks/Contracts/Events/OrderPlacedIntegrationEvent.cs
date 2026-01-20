using TicketSalesPlatform.Contracts.Dtos;

namespace TicketSalesPlatform.Contracts.Events
{
    public sealed record OrderPlacedIntegrationEvent(
        Guid OrderId,
        Guid CustomerId,
        decimal TotalPrice,
        DateTime OrderDate,
        List<OrderItemDto> Items
    );
}
