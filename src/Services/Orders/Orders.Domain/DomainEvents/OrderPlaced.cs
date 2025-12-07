using SharedKernel;

namespace TicketSalesPlatform.Orders.Domain.DomainEvents
{
    public sealed record OrderPlaced(
        Guid OrderId,
        Guid CustomerId,
        decimal TotalPrice,
        List<OrderPlaced.OrderItemData> Items
    ) : IDomainEvent
    {
        public sealed record OrderItemData(
            Guid TicketTypeId,
            string EventName,
            decimal UnitPrice,
            int Quantity
        );

        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
