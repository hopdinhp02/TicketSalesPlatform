using Marten.Events.Aggregation;
using TicketSalesPlatform.Orders.Domain.DomainEvents;
using TicketSalesPlatform.Orders.Domain.Enums;
using TicketSalesPlatform.Orders.Domain.ReadModels;

namespace TicketSalesPlatform.Orders.Infrastructure.Projections
{
    public class OrderDetailsProjection : SingleStreamProjection<OrderDetails, Guid>
    {
        public OrderDetailsProjection() { }

        public OrderDetails Create(OrderPlaced e)
        {
            var items = e
                .Items.Select(i => new OrderItemDetails
                {
                    TicketTypeId = i.TicketTypeId,
                    EventName = i.EventName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                })
                .ToList();

            return new OrderDetails
            {
                Id = e.OrderId,
                CustomerId = e.CustomerId,
                TotalPrice = e.TotalPrice,
                Status = OrderStatus.Placed.ToString(),
                PlacedOn = e.OccurredOn,
                Items = items,
            };
        }

        public void Apply(OrderPaid e, OrderDetails current)
        {
            current.Status = OrderStatus.Paid.ToString();
        }

        public void Apply(OrderCancelled e, OrderDetails current)
        {
            current.Status = OrderStatus.Cancelled.ToString();
            current.CancellationReason = e.Reason;
        }
    }
}
