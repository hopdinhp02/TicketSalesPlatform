using MediatR;

namespace TicketFlow.Orders.Application.PlaceOrder
{
    public sealed record PlaceOrderCommand(Guid CustomerId, List<OrderItemDto> Items)
        : IRequest<Guid>;

    public sealed record OrderItemDto(
        Guid TicketTypeId,
        string EventName,
        decimal UnitPrice,
        int Quantity
    );
}
