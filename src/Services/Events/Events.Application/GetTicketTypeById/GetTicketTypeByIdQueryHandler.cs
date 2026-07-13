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
            var view = await _session.LoadAsync<TicketTypeView>(request.TicketTypeId, token);

            if (view is null || !view.IsPublished)
                return null;

            return new TicketTypeDto(view.Id, view.EventId, view.Name, view.Price, view.Quantity);
        }
    }
}
