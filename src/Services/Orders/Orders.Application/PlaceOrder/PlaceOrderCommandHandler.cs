using MediatR;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Application.Clients;
using TicketSalesPlatform.Orders.Domain.Aggregates;
using TicketSalesPlatform.Orders.Domain.ValueObjects;

namespace TicketSalesPlatform.Orders.Application.PlaceOrder
{
    public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Guid>
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventsClient _eventsClient;
        private readonly IInventoryClient _inventoryClient;
        private readonly ILogger<PlaceOrderCommandHandler> _logger;

        public PlaceOrderCommandHandler(
            IRepository<Order> orderRepository,
            IUnitOfWork unitOfWork,
            IEventsClient eventsClient,
            IInventoryClient inventoryClient,
            ILogger<PlaceOrderCommandHandler> logger
        )
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _eventsClient = eventsClient;
            _inventoryClient = inventoryClient;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            PlaceOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            var orderItemsEntity = new List<InitialOrderItem>();

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
                    new InitialOrderItem(
                        ticketInfo.Id,
                        ticketInfo.Name,
                        ticketInfo.Price,
                        itemDto.Quantity
                    )
                );
            }

            var order = Order.Initialize(request.CustomerId, orderItemsEntity);

            _logger.LogInformation("Creating new order {@Order}", order);

            _orderRepository.Add(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return order.Id;
        }
    }
}
