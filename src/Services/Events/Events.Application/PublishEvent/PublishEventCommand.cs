using MediatR;

namespace TicketSalesPlatform.Events.Application.PublishEvent
{
    public record PublishEventCommand(Guid EventId) : IRequest;
}
