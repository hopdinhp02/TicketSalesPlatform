using MediatR;
using TicketFlow.Orders.Domain.ReadModels;

namespace TicketFlow.Orders.Application.GetOrderById
{
    public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetails?>;
}
