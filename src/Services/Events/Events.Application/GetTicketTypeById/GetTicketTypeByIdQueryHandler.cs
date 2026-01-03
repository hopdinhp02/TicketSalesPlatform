using Marten;
using MediatR;
using TicketSalesPlatform.Events.Domain.ReadModels;

namespace TicketSalesPlatform.Events.Application.GetTicketTypeById
{
    public class GetTicketTypeByIdQueryHandler
        : IRequestHandler<GetTicketTypeByIdQuery, TicketTypeDto?>
    {
        private readonly IQuerySession _session;

        public GetTicketTypeByIdQueryHandler(IQuerySession session)
        {
            _session = session;
        }

        public async Task<TicketTypeDto?> Handle(
            GetTicketTypeByIdQuery request,
            CancellationToken token
        )
        {
            var view = await _session
                .Query<EventDetailView>()
                .Where(v => v.IsPublished && v.TicketTypes.Any(t => t.Id == request.TicketTypeId))
                .FirstOrDefaultAsync(token);

            if (view is null)
                return null;

            return view
                .TicketTypes.Select(x => new TicketTypeDto(
                    x.Id,
                    x.EventId,
                    x.Name,
                    x.Price,
                    x.Quantity
                ))
                .FirstOrDefault(t => t.Id == request.TicketTypeId);
        }
    }
}
