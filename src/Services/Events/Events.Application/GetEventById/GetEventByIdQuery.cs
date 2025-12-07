using TicketSalesPlatform.Events.Domain.ReadModels;
using MediatR;

namespace TicketSalesPlatform.Events.Application.GetEventById
{
    public sealed record GetEventByIdQuery(Guid Id) : IRequest<EventSummary?>;
}
