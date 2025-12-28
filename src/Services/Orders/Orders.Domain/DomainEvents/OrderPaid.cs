using SharedKernel;

namespace TicketSalesPlatform.Orders.Domain.DomainEvents
{
    public record OrderPaid(Guid OrderId) : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    };
}
