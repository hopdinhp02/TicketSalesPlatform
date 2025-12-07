using SharedKernel;
using NewId = MassTransit.NewId;

namespace TicketSalesPlatform.Orders.Domain.Aggregates
{
    public sealed class OrderItem : Entity<Guid>
    {
        public Guid OrderId { get; private set; }
        public Guid TicketTypeId { get; private set; }
        public string EventName { get; private set; }
        public decimal UnitPrice { get; private set; }
        public int Quantity { get; private set; }

        public OrderItem(
            Guid orderId,
            Guid ticketTypeId,
            string eventName,
            decimal unitPrice,
            int quantity
        )
            : base(NewId.NextGuid())
        {
            // Business rule validations can go here
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));
            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

            OrderId = orderId;
            TicketTypeId = ticketTypeId;
            EventName = eventName;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }
    }
}
