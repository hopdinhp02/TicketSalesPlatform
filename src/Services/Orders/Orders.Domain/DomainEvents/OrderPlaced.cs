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
        public record OrderItemData(
            Guid ItemId,
            Guid TicketTypeId,
            string Name,
            decimal UnitPrice,
            int Quantity
        );

        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
