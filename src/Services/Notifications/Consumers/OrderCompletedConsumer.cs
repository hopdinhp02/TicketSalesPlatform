using MassTransit;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Notifications.Api.Idempotency;

namespace TicketSalesPlatform.Notifications.Api.Consumers
{
    public sealed class OrderCompletedConsumer : IConsumer<OrderCompletedIntegrationEvent>
    {
        private readonly ILogger<OrderCompletedConsumer> _logger;
        private readonly IProcessedMessageService _processedMessageService;

        public OrderCompletedConsumer(
            ILogger<OrderCompletedConsumer> logger,
            IProcessedMessageService processedMessageService
        )
        {
            _logger = logger;
            _processedMessageService = processedMessageService;
        }

        public async Task Consume(ConsumeContext<OrderCompletedIntegrationEvent> context)
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

            _logger.LogInformation(
                "--- Ticket Issuance Notice (Order Completed) --- \n"
                    + "OrderId: {OrderId} \n"
                    + "Customer: {CustomerId} \n"
                    + "Total Paid: ${TotalPrice} \n"
                    + "Completed At: {CompletedAt} \n"
                    + "Status: E-Tickets Issued & Ready in Account! \n"
                    + "------------------------------------------------",
                message.OrderId,
                message.CustomerId,
                message.TotalPrice,
                message.CompletedAt
            );

            if (context.MessageId.HasValue)
            {
                await _processedMessageService.MarkAsProcessedAsync(context.MessageId.Value);
            }
        }
    }
}
