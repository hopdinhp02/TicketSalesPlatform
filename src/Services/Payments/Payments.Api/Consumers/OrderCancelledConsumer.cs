using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.IntegrationEvents;
using TicketSalesPlatform.Payments.Api.Data;
using TicketSalesPlatform.Payments.Api.Entities;

namespace TicketSalesPlatform.Payments.Api.Consumers
{
    public class OrderCancelledConsumer : IConsumer<OrderCancelledIntegrationEvent>
    {
        private readonly ILogger<OrderCancelledConsumer> _logger;
        private readonly PaymentDbContext _dbContext;

        public OrderCancelledConsumer(
            ILogger<OrderCancelledConsumer> logger,
            PaymentDbContext dbContext
        )
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<OrderCancelledIntegrationEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation(
                "Payment Service: Received Cancel Order {OrderId}. Reason: {Reason}",
                message.OrderId,
                message.Reason
            );

            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p =>
                p.OrderId == message.OrderId
            );

            if (payment is null)
            {
                _logger.LogWarning(
                    "Order {OrderId} cancelled but no Payment record found.",
                    message.OrderId
                );
                return;
            }

            if (payment.Status == PaymentStatus.Completed)
            {
                _logger.LogCritical(
                    "CONFLICT: Order {OrderId} is Cancelled but Payment is COMPLETED. Initiating REFUND...",
                    message.OrderId
                );

                payment.Refund($"Order Cancelled after Payment: {message.Reason}");
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "AUTO-REFUNDED Payment {PaymentId} for Order {OrderId}",
                    payment.Id,
                    message.OrderId
                );
                return;
            }

            if (payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.Failed)
            {
                payment.Cancel($"Order Cancelled: {message.Reason}");
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Payment CANCELLED for Order {OrderId}", message.OrderId);
                return;
            }

            // IDEMPOTENCY
            if (
                payment.Status == PaymentStatus.Cancelled
                || payment.Status == PaymentStatus.Refunded
            )
            {
                _logger.LogInformation(
                    "Payment for Order {OrderId} is already processed ({Status}). Skipping.",
                    message.OrderId,
                    payment.Status
                );
                return;
            }
        }
    }
}
