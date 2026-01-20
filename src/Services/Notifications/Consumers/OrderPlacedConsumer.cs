using MassTransit;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Notifications.Api.Idempotency;

namespace TicketSalesPlatform.Notifications.Api.Consumers
{
    public sealed class OrderPlacedConsumer : IConsumer<OrderPlacedIntegrationEvent>
    {
        private readonly ILogger<OrderPlacedConsumer> _logger;
        private readonly IProcessedMessageService _processedMessageService;

        public OrderPlacedConsumer(
            ILogger<OrderPlacedConsumer> logger,
            IProcessedMessageService processedMessageService
        )
        {
            _logger = logger;
            _processedMessageService = processedMessageService;
        }

        /// <summary>
        /// This method is called by MassTransit whenever a new OrderPlacedIntegrationEvent
        /// message arrives in the queue.
        /// </summary>
        public async Task Consume(ConsumeContext<OrderPlacedIntegrationEvent> context)
        {
            // Use the unique MessageId provided by MassTransit for the check.
            if (
                context.MessageId.HasValue
                && await _processedMessageService.HasBeenProcessedAsync(context.MessageId.Value)
            )
            {
                _logger.LogWarning(
                    "Duplicate message received, skipping. MessageId: {MessageId}",
                    context.MessageId
                );
                return;
            }

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

            // Mark the message as processed after successfully handling it.
            if (context.MessageId.HasValue)
            {
                await _processedMessageService.MarkAsProcessedAsync(context.MessageId.Value);
            }
        }
    }
}
