using Marten;
using MediatR;
using TicketSalesPlatform.Events.Domain.ReadModels;

namespace TicketSalesPlatform.Events.Application.GetEventById
{
    public sealed class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventSummary?>
    {
        private readonly IQuerySession _querySession;

        public GetEventByIdQueryHandler(IQuerySession querySession)
        {
            _querySession = querySession;
        }

        public async Task<EventSummary?> Handle(
            GetEventByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            // This is much more efficient than loading the full aggregate.
            // It queries the projected document directly.
            var eventSummary = await _querySession.LoadAsync<EventSummary>(
                request.Id,
                cancellationToken
            );

            return eventSummary;
        }
    }
}
