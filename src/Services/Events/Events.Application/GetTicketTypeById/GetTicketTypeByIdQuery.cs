using MediatR;

namespace TicketSalesPlatform.Events.Application.GetTicketTypeById
{
    public record GetTicketTypeByIdQuery(Guid TicketTypeId) : IRequest<TicketTypeDto?>;

    public record TicketTypeDto(Guid Id, Guid EventId, string Name, decimal Price, int Quantity);
}
