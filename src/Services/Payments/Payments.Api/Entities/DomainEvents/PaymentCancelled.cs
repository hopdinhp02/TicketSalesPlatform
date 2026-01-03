using SharedKernel;

namespace TicketSalesPlatform.Payments.Api.Entities.DomainEvents
{
    public record PaymentCancelled(Guid PaymentId, Guid OrderId, string Reason) : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
