using Marten.Events.Aggregation;
using TicketFlow.Orders.Domain.DomainEvents;
using TicketFlow.Orders.Domain.Enums;
using TicketFlow.Orders.Domain.ReadModels;

namespace TicketFlow.Orders.Infrastructure.Projections
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
    }
}
