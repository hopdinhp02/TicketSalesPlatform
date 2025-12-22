using SharedKernel;

namespace TicketSalesPlatform.Payments.Api.Entities.DomainEvents
{
    public sealed record PaymentCompleted(Guid PaymentId, Guid OrderId, decimal Amount)
        : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
