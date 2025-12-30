using MassTransit;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.IntegrationEvents;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Domain.Aggregates;

namespace TicketSalesPlatform.Orders.Application.Consumers
{
    public class PaymentRefundedConsumer : IConsumer<PaymentRefundedIntegrationEvent>
    {
        private readonly IRepository<Order> _repository; // Marten repository
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentRefundedConsumer> _logger;

        public PaymentRefundedConsumer(
            IRepository<Order> repository,
            IUnitOfWork unitOfWork,
            ILogger<PaymentRefundedConsumer> logger
        )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentRefundedIntegrationEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation(
                "Orders: Received Payment Refunded for Order {OrderId}. Updating status...",
                message.OrderId
            );

            var order = await _repository.GetByIdAsync(message.OrderId);

            if (order is null)
            {
                _logger.LogError(
                    "Orders: Order {OrderId} not found processing Refund event.",
                    message.OrderId
                );
                return;
            }

            try
            {
                order.MarkAsRefunded();

                _repository.Update(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Orders: Order {OrderId} marked as REFUNDED.",
                    message.OrderId
                );
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    "Orders: Failed to mark Order {OrderId} as Refunded. Reason: {Error}",
                    message.OrderId,
                    ex.Message
                );
            }
        }
    }
}
