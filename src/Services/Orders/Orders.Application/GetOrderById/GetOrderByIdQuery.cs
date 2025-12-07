using MediatR;
using TicketSalesPlatform.Orders.Domain.ReadModels;

namespace TicketSalesPlatform.Orders.Application.GetOrderById
{
    public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetails?>;
}
