using TicketFlow.Events.Domain.ReadModels;
using MediatR;

namespace TicketFlow.Events.Application.GetEventById
{
    public sealed record GetEventByIdQuery(Guid Id) : IRequest<EventSummary?>;
}
