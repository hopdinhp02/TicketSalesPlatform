using MassTransit;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Notifications.Api.Idempotency;

namespace TicketSalesPlatform.Notifications.Api.Consumers
{
    public sealed class OrderFailedConsumer : IConsumer<OrderFailedIntegrationEvent>
    {
        private readonly ILogger<OrderFailedConsumer> _logger;
        private readonly IProcessedMessageService _processedMessageService;

        public OrderFailedConsumer(
            ILogger<OrderFailedConsumer> logger,
            IProcessedMessageService processedMessageService
        )
        {
            _logger = logger;
            _processedMessageService = processedMessageService;
        }

        public async Task Consume(ConsumeContext<OrderFailedIntegrationEvent> context)
        {
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

            _logger.LogWarning(
                "--- Order Failure Notice --- \n"
                    + "OrderId: {OrderId} \n"
                    + "Customer: {CustomerId} \n"
                    + "Reason: {Reason} \n"
                    + "Failed At: {FailedAt} \n"
                    + "Status: Order Failed. Any temporary holds/reservations have been released. \n"
                    + "----------------------------",
                message.OrderId,
                message.CustomerId,
                message.Reason,
                message.FailedAt
            );

            if (context.MessageId.HasValue)
            {
                await _processedMessageService.MarkAsProcessedAsync(context.MessageId.Value);
            }
        }
    }
}
