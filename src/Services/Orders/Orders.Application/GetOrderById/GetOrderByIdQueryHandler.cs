using Marten;
using MediatR;
using TicketFlow.Orders.Domain.ReadModels;

namespace TicketFlow.Orders.Application.GetOrderById
{
    public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetails?>
    {
        private readonly IQuerySession _querySession;

        public GetOrderByIdQueryHandler(IQuerySession querySession) => _querySession = querySession;

        public async Task<OrderDetails?> Handle(
            GetOrderByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            return await _querySession.LoadAsync<OrderDetails>(request.Id, cancellationToken);
        }
    }
}
