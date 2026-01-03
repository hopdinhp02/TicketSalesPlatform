using SharedKernel;

namespace TicketSalesPlatform.Events.Domain.DomainEvents
{
    public record EventPublished(Guid EventId) : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
