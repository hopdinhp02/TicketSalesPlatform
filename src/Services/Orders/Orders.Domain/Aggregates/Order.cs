using SharedKernel;
using TicketFlow.Orders.Domain.Aggregates;
using TicketFlow.Orders.Domain.DomainEvents;
using TicketFlow.Orders.Domain.Enums;
using NewId = MassTransit.NewId;

namespace Orders.Domain.Aggregates
{
    public sealed class Order : AggregateRoot<Guid>
    {
        private readonly List<OrderItem> _orderItems = new();

        public Guid CustomerId { get; private set; }
        public OrderStatus Status { get; private set; }
        public decimal TotalPrice => _orderItems.Sum(item => item.UnitPrice * item.Quantity);
        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

        private Order(Guid id, Guid customerId)
            : base(id)
        {
            CustomerId = customerId;
        }

        /// <summary>
        /// Factory method for creating a new Order from a cart/basket.
        /// The order is created directly in a 'Placed' state.
        /// </summary>
        public static Order Create(Guid customerId, List<OrderItem> items)
        {
            // Invariant: Cannot create an order with no items.
            if (items is null || !items.Any())
            {
                throw new InvalidOperationException("Cannot create an order with no items.");
            }

            var order = new Order(NewId.NextGuid(), customerId);

            foreach (var item in items)
            {
                order._orderItems.Add(
                    new OrderItem(
                        order.Id,
                        item.TicketTypeId,
                        item.EventName,
                        item.UnitPrice,
                        item.Quantity
                    )
                );
            }

            order.Status = OrderStatus.Placed;

            var eventItems = order
                .OrderItems.Select(oi => new OrderPlaced.OrderItemData(
                    oi.TicketTypeId,
                    oi.EventName,
                    oi.UnitPrice,
                    oi.Quantity
                ))
                .ToList();

            order.AddDomainEvent(
                new OrderPlaced(order.Id, order.CustomerId, order.TotalPrice, eventItems)
            );

            return order;
        }
    }
}
