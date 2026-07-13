using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Application.Clients;
using TicketSalesPlatform.Orders.Domain.ValueObjects;
using Order = TicketSalesPlatform.Orders.Domain.Aggregates.Order;

namespace TicketSalesPlatform.Orders.Application.PlaceOrder
{
    public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Guid>
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventsClient _eventsClient;
        private readonly IInventoryClient _inventoryClient;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<PlaceOrderCommandHandler> _logger;

        public PlaceOrderCommandHandler(
            IRepository<Order> orderRepository,
            IUnitOfWork unitOfWork,
            IEventsClient eventsClient,
            IInventoryClient inventoryClient,
            IConnectionMultiplexer redis,
            ILogger<PlaceOrderCommandHandler> logger
        )
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _eventsClient = eventsClient;
            _inventoryClient = inventoryClient;
            _redis = redis;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            PlaceOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            var ticketTypeIds = request.Items.Select(x => x.TicketTypeId).Distinct().ToList();
            var ticketTypes = await _eventsClient.GetTicketTypesBulkAsync(
                ticketTypeIds,
                cancellationToken
            );
            var ticketTypeDict = ticketTypes.ToDictionary(t => t.Id);

            var orderItemsEntity = request
                .Items.Select(itemDto =>
                {
                    if (!ticketTypeDict.TryGetValue(itemDto.TicketTypeId, out var ticketInfo))
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
                })
                .ToList();

            var redisDb = _redis.GetDatabase();

            foreach (var item in request.Items)
            {
                var redisKey = $"inventory:tickettype:{item.TicketTypeId}:available";
                var keyExists = await redisDb.KeyExistsAsync(redisKey);
                if (!keyExists)
                {
                    var hasStock = await _inventoryClient.CheckStockAsync(
                        item.TicketTypeId,
                        item.Quantity,
                        cancellationToken
                    );

                    if (!hasStock)
                    {
                        throw new InvalidOperationException(
                            $"Out of stock for TicketType {item.TicketTypeId}"
                        );
                    }
                }
            }

            var script =
                @"
                for i, key in ipairs(KEYS) do
                    local qty = tonumber(ARGV[i])
                    local current = tonumber(redis.call('GET', key) or '0')
                    if current < qty then
                        return key
                    end
                end

                for i, key in ipairs(KEYS) do
                    local qty = tonumber(ARGV[i])
                    redis.call('DECRBY', key, qty)
                end

                return 'OK'
                ";
            var keys = request
                .Items.Select(i => (RedisKey)$"inventory:tickettype:{i.TicketTypeId}:available")
                .ToArray();
            var args = request.Items.Select(i => (RedisValue)i.Quantity.ToString()).ToArray();

            var result = (string?)await redisDb.ScriptEvaluateAsync(script, keys, args);
            if (result != "OK")
            {
                throw new InvalidOperationException($"Out of stock for {result}");
            }

            var order = Order.Initialize(request.CustomerId, orderItemsEntity);

            _logger.LogInformation("Creating new order {@Order}", order);

            _orderRepository.Add(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return order.Id;
        }
    }
}
