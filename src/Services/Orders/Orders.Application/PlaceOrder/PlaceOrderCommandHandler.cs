using MediatR;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Application.Clients;
using TicketSalesPlatform.Orders.Domain.Aggregates;
using TicketSalesPlatform.Orders.Domain.ValueObjects;
using StackExchange.Redis;
using Order = TicketSalesPlatform.Orders.Domain.Aggregates.Order;

namespace TicketSalesPlatform.Orders.Application.PlaceOrder
{
    public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Guid>
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventsClient _eventsClient;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<PlaceOrderCommandHandler> _logger;

        public PlaceOrderCommandHandler(
            IRepository<Order> orderRepository,
            IUnitOfWork unitOfWork,
            IEventsClient eventsClient,
            IConnectionMultiplexer redis,
            ILogger<PlaceOrderCommandHandler> logger
        )
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _eventsClient = eventsClient;
            _redis = redis;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            PlaceOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            var validationTasks = request.Items.Select(async itemDto =>
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

                return new InitialOrderItem(
                    ticketInfo.Id,
                    ticketInfo.Name,
                    ticketInfo.Price,
                    itemDto.Quantity
                );
            });

            var orderItemsArray = await Task.WhenAll(validationTasks);
            var orderItemsEntity = orderItemsArray.ToList();

            // Perform synchronous Redis stock check & atomic decrement
            var redisDb = _redis.GetDatabase();
            var decrementedItems = new List<(Guid TicketTypeId, int Quantity)>();
            bool outOfStock = false;
            string failedTicketTypeId = "";

            foreach (var item in request.Items)
            {
                var redisKey = $"inventory:tickettype:{item.TicketTypeId}:available";
                var remaining = await redisDb.StringDecrementAsync(redisKey, item.Quantity);
                if (remaining < 0)
                {
                    // Revert decrement
                    await redisDb.StringIncrementAsync(redisKey, item.Quantity);
                    outOfStock = true;
                    failedTicketTypeId = item.TicketTypeId.ToString();
                    break;
                }
                decrementedItems.Add((item.TicketTypeId, item.Quantity));
            }

            if (outOfStock)
            {
                // Rollback other decrements
                foreach (var dec in decrementedItems)
                {
                    await redisDb.StringIncrementAsync($"inventory:tickettype:{dec.TicketTypeId}:available", dec.Quantity);
                }
                throw new InvalidOperationException($"Out of stock for TicketType {failedTicketTypeId}");
            }

            var order = Order.Initialize(request.CustomerId, orderItemsEntity);

            _logger.LogInformation("Creating new order {@Order}", order);

            _orderRepository.Add(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return order.Id;
        }
    }
}
