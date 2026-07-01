using MassTransit;
using StackExchange.Redis;
using TicketSalesPlatform.Contracts.Commands;
using TicketSalesPlatform.Contracts.Events;

namespace TicketSalesPlatform.Inventory.Api.Consumers
{
    public class ReserveStockFaultConsumer : IConsumer<Fault<ReserveStockCommand>>
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<ReserveStockFaultConsumer> _logger;

        public ReserveStockFaultConsumer(IConnectionMultiplexer redis, ILogger<ReserveStockFaultConsumer> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<Fault<ReserveStockCommand>> context)
        {
            var command = context.Message.Message;
            var exceptions = context.Message.Exceptions;
            var errorReason = exceptions.FirstOrDefault()?.Message ?? "Unknown inventory reservation error";

            _logger.LogWarning(
                "ReserveStockCommand faulted for Order {OrderId}. Reason: {Reason}. Rolling back Redis stock...",
                command.OrderId,
                errorReason
            );

            var redisDb = _redis.GetDatabase();

            if (command.Items != null)
            {
                foreach (var item in command.Items)
                {
                    var redisKey = $"inventory:tickettype:{item.TicketTypeId}:available";
                    await redisDb.StringIncrementAsync(redisKey, item.Quantity);
                    _logger.LogInformation(
                        "Rolled back Redis stock for TicketType {TicketTypeId} by {Quantity}",
                        item.TicketTypeId,
                        item.Quantity
                    );
                }
            }

            await context.Publish(new OrderReservationFailedIntegrationEvent(
                command.OrderId,
                errorReason
            ));

            _logger.LogInformation("Published OrderReservationFailedIntegrationEvent for Order {OrderId}", command.OrderId);
        }
    }
}
