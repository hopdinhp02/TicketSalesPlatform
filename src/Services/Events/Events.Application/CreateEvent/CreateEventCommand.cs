using MediatR;

namespace TicketFlow.Events.Application.CreateEvent
{
    public sealed record CreateEventCommand(string Title, string Description, DateTime Date)
        : IRequest<Guid>;
}
