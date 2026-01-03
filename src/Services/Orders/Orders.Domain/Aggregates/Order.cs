using SharedKernel;
using TicketSalesPlatform.Orders.Domain.DomainEvents;
using TicketSalesPlatform.Orders.Domain.Enums;
using TicketSalesPlatform.Orders.Domain.ValueObjects;
using NewId = MassTransit.NewId;

namespace TicketSalesPlatform.Orders.Domain.Aggregates
{
    public sealed class Order : AggregateRoot<Guid>
    {
        private readonly List<OrderItem> _orderItems = new();

        public Guid CustomerId { get; private set; }
        public OrderStatus Status { get; private set; }
        public decimal TotalPrice => _orderItems.Sum(item => item.UnitPrice * item.Quantity);
        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

        private Order()
            : base(Guid.Empty) { }

        public static Order Initialize(Guid customerId, List<InitialOrderItem> items)
        {
            if (items is null || !items.Any())
                throw new InvalidOperationException("Cannot create an order with no items.");

            var orderId = NewId.NextGuid();

            var eventItems = items
                .Select(i => new OrderPlaced.OrderItemData(
                    NewId.NextGuid(),
                    i.TicketTypeId,
                    i.Name,
                    i.UnitPrice,
                    i.Quantity
                ))
                .ToList();

            var totalPrice = eventItems.Sum(x => x.UnitPrice * x.Quantity);
            var @event = new OrderPlaced(orderId, customerId, totalPrice, eventItems);

            var order = new Order();
            order.AddDomainEvent(@event);
            order.Apply(@event);

            return order;
        }

        public void MarkAsPaid()
        {
            if (Status == OrderStatus.Paid)
                return;
            if (Status == OrderStatus.Cancelled)
                throw new InvalidOperationException("Cannot pay for a cancelled order.");

            var @event = new OrderPaid(Id);
            AddDomainEvent(@event);
            Apply(@event);
        }

        public void MarkAsCancelled(string reason)
        {
            if (Status == OrderStatus.Paid)
                throw new InvalidOperationException("Cannot cancel a paid order.");
            if (Status == OrderStatus.Cancelled)
                return;

            var @event = new OrderCancelled(Id, reason);
            AddDomainEvent(@event);
            Apply(@event);
        }

        public void MarkAsRefunded()
        {
            if (Status == OrderStatus.Refunded)
                return;

            if (Status != OrderStatus.Paid)
            {
                throw new InvalidOperationException(
                    $"Cannot refund Order {Id} because status is {Status}. Only Paid orders can be refunded."
                );
            }

            var @event = new OrderRefunded(Id);
            AddDomainEvent(@event);
            Apply(@event);
        }

        private void Apply(OrderPlaced @event)
        {
            Id = @event.OrderId;
            CustomerId = @event.CustomerId;
            Status = OrderStatus.Placed;

            _orderItems.AddRange(
                @event
                    .Items.Select(i => new OrderItem(
                        i.ItemId,
                        @event.OrderId,
                        i.TicketTypeId,
                        i.Name,
                        i.UnitPrice,
                        i.Quantity
                    ))
                    .ToList()
            );
        }

        private void Apply(OrderPaid @event)
        {
            Status = OrderStatus.Paid;
        }

        private void Apply(OrderCancelled @event)
        {
            Status = OrderStatus.Cancelled;
        }

        private void Apply(OrderRefunded @event)
        {
            Status = OrderStatus.Refunded;
        }
    }
}
