using MediatR;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Application.Clients;
using TicketSalesPlatform.Orders.Domain.Aggregates;

namespace TicketSalesPlatform.Orders.Application.PlaceOrder
{
    public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Guid>
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;
        private readonly IEventsClient _eventsClient;
        private readonly IInventoryClient _inventoryClient;
        private readonly ILogger<PlaceOrderCommandHandler> _logger;

        public PlaceOrderCommandHandler(
            IRepository<Order> orderRepository,
            IUnitOfWork unitOfWork,
            IPublisher publisher,
            IEventsClient eventsClient,
            IInventoryClient inventoryClient,
            ILogger<PlaceOrderCommandHandler> logger
        )
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _publisher = publisher;
            _eventsClient = eventsClient;
            _inventoryClient = inventoryClient;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            PlaceOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            var orderItemsEntity = new List<OrderItem>();

            foreach (var itemDto in request.Items)
            {
                var ticketInfo = await _eventsClient.GetTicketTypeAsync(
                    itemDto.TicketTypeId,
                    cancellationToken
                );

                if (ticketInfo is null)
                {
                    throw new InvalidOperationException(
                        $"TicketType {itemDto.TicketTypeId} does not exist or is invalid."
                    );
                }

                var hasStock = await _inventoryClient.CheckStockAsync(
                    itemDto.TicketTypeId,
                    itemDto.Quantity,
                    cancellationToken
                );

                if (!hasStock)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for ticket: {ticketInfo.Name}. Request: {itemDto.Quantity}"
                    );
                }

                orderItemsEntity.Add(
                    new OrderItem(
                        Guid.Empty,
                        ticketInfo.Id,
                        ticketInfo.EventName,
                        ticketInfo.Price,
                        itemDto.Quantity
                    )
                );
            }

            var order = Order.Create(request.CustomerId, orderItemsEntity);

            _logger.LogInformation("Creating new order {@Order}", order);

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
