using MediatR;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Domain.Aggregates;

namespace TicketSalesPlatform.Orders.Application.PlaceOrder
{
    public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Guid>
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;
        private readonly IEventsClient _eventsClient;
        private readonly ILogger<PlaceOrderCommandHandler> _logger;

        public PlaceOrderCommandHandler(
            IRepository<Order> orderRepository,
            IUnitOfWork unitOfWork,
            IPublisher publisher,
            IEventsClient eventsClient,
            ILogger<PlaceOrderCommandHandler> logger
        )
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _publisher = publisher;
            _eventsClient = eventsClient;
            _logger = logger;
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

            _logger.LogInformation("Creating new order {@Order}", order);

            _orderRepository.Add(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var domainEvent in order.GetDomainEvents())
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
            order.ClearDomainEvents();

            _logger.LogInformation("Order created successfully with Id: {OrderId}", order.Id);
            return order.Id;
        }
    }
}
