using MediatR;
using Orders.Domain.Aggregates;
using TicketFlow.Orders.Application.Abstractions;
using TicketFlow.Orders.Domain.Aggregates;

namespace TicketFlow.Orders.Application.PlaceOrder
{
    public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Guid>
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;
        private readonly IEventsClient _eventsClient;

        public PlaceOrderCommandHandler(
            IRepository<Order> orderRepository,
            IUnitOfWork unitOfWork,
            IPublisher publisher,
            IEventsClient eventsClient
        )
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _publisher = publisher;
            _eventsClient = eventsClient;
        }

        public async Task<Guid> Handle(
            PlaceOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            foreach (var item in request.Items)
            {
                // This is a placeholder for the eventId
                var eventId = Guid.NewGuid();

                var availability = await _eventsClient.GetTicketAvailabilityAsync(
                    eventId,
                    item.TicketTypeId,
                    cancellationToken
                );

                if (availability is null || availability.AvailableQuantity < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Not enough tickets available for TicketTypeId: {item.TicketTypeId}"
                    );
                }
            }

            var orderItems = request
                .Items.Select(item => new OrderItem(
                    Guid.Empty,
                    item.TicketTypeId,
                    item.EventName,
                    item.UnitPrice,
                    item.Quantity
                ))
                .ToList();
            var order = Order.Create(request.CustomerId, orderItems);
            _orderRepository.Add(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            foreach (var domainEvent in order.GetDomainEvents())
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
            order.ClearDomainEvents();
            return order.Id;
        }
    }
}
