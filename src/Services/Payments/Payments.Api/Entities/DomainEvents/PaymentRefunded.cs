using SharedKernel;

namespace TicketSalesPlatform.Payments.Api.Entities.DomainEvents
{
    public sealed record PaymentRefunded(Guid PaymentId, Guid OrderId) : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
