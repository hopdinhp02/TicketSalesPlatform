using SharedKernel;

namespace TicketSalesPlatform.Orders.Domain.DomainEvents
{
    public record OrderCancelled(Guid OrderId, string Reason) : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
