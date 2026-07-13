using MediatR;

namespace TicketSalesPlatform.Events.Application.GetTicketTypesBulk
{
    public record GetTicketTypesBulkQuery(IEnumerable<Guid> TicketTypeIds)
        : IRequest<IEnumerable<GetTicketTypeById.TicketTypeDto>>;
}
