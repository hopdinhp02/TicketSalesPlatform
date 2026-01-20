using TicketSalesPlatform.Contracts.Dtos;

namespace TicketSalesPlatform.Contracts.Commands
{
    public record ReserveStockCommand(Guid OrderId, Guid CustomerId, List<OrderItemDto> Items);
}
