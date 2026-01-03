using SharedKernel;

namespace TicketSalesPlatform.Orders.Domain.Aggregates
{
    public sealed class OrderItem : Entity<Guid>
    {
        public Guid OrderId { get; private set; }
        public Guid TicketTypeId { get; private set; }
        public string EventName { get; private set; }
        public decimal UnitPrice { get; private set; }
        public int Quantity { get; private set; }

        private OrderItem()
            : base(Guid.Empty) { }

        public OrderItem(
            Guid id,
            Guid orderId,
            Guid ticketTypeId,
            string eventName,
            decimal unitPrice,
            int quantity
        )
            : base(id)
        {
            OrderId = orderId;
            TicketTypeId = ticketTypeId;
            EventName = eventName;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }
    }
}
