using SharedKernel;

namespace TicketSalesPlatform.Events.Domain.DomainEvents
{
    public record TicketTypeAdded(
        Guid EventId,
        Guid TicketTypeId,
        string Name,
        decimal Price,
        int Quantity
    ) : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
