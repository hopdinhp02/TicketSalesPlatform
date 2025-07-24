using MassTransit;
using TicketFlow.IntegrationEvents;

namespace TicketFlow.Notifications.Api.Consumers
{
    public sealed class OrderPlacedConsumer : IConsumer<OrderPlacedIntegrationEvent>
    {
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This method is called by MassTransit whenever a new OrderPlacedIntegrationEvent
        /// message arrives in the queue.
        /// </summary>
        public Task Consume(ConsumeContext<OrderPlacedIntegrationEvent> context)
        {
            var message = context.Message;

            // Simulate sending a confirmation email.
            _logger.LogInformation(
                "--- Sending Order Confirmation --- \n"
                    + "OrderId: {OrderId} \n"
                    + "Customer: {CustomerId} \n"
                    + "Total Price: {TotalPrice} \n"
                    + "---------------------------------",
                message.OrderId,
                message.CustomerId,
                message.TotalPrice
            );

            return Task.CompletedTask;
        }
    }
}
