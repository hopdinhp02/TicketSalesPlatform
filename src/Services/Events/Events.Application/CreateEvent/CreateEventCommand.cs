using MediatR;

namespace TicketSalesPlatform.Events.Application.CreateEvent
{
    public record CreateEventCommand(
        string Title,
        string Description,
        DateTime Date,
        List<TicketTypeInput> TicketTypes
    ) : IRequest<Guid>;

    public record TicketTypeInput(string Name, decimal Price, int Quantity);
}
