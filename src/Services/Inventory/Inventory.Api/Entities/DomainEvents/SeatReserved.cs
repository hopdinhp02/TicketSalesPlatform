using SharedKernel;

namespace TicketSalesPlatform.Inventory.Api.Entities.DomainEvents
{
    public sealed record SeatReserved(Guid SeatId, Guid UserId, Guid OrderId, DateTime ExpiresAt)
        : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
