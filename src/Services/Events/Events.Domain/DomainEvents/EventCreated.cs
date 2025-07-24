using SharedKernel;

namespace TicketFlow.Events.Domain.DomainEvents
{
    public sealed record EventCreated(Guid EventId, string Title, DateTime Date) : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
