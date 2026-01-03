using MassTransit;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.IntegrationEvents;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Domain.Aggregates;

namespace TicketSalesPlatform.Orders.Application.Consumers
{
    public class PaymentCancelledConsumer : IConsumer<PaymentCancelledIntegrationEvent>
    {
        private readonly IRepository<Order> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentCancelledConsumer> _logger;

        public PaymentCancelledConsumer(
            IRepository<Order> repository,
            IUnitOfWork unitOfWork,
            ILogger<PaymentCancelledConsumer> logger
        )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentCancelledIntegrationEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation(
                "Orders: Received Payment CANCELLED for Order {OrderId}. Reason: {Reason}. Updating status...",
                message.OrderId,
                message.Reason
            );

            var order = await _repository.GetByIdAsync(message.OrderId);

            if (order is null)
            {
                _logger.LogError(
                    "Orders: Order {OrderId} not found processing Cancelled event.",
                    message.OrderId
                );
                return;
            }

            try
            {
                order.MarkAsCancelled(message.Reason);

                _repository.Update(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Orders: Order {OrderId} marked as CANCELLED.",
                    message.OrderId
                );
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    "Orders: Failed to mark Order {OrderId} as Cancelled. Reason: {Error}",
                    message.OrderId,
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing Payment Cancelled event for Order {OrderId}",
                    message.OrderId
                );
                throw;
            }
        }
    }
}
