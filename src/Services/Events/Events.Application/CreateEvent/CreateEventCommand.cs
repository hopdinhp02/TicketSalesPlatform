using MediatR;

namespace TicketSalesPlatform.Events.Application.CreateEvent
{
    public sealed record CreateEventCommand(string Title, string Description, DateTime Date)
        : IRequest<Guid>;
}
