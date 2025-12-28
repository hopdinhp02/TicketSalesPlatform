using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.IntegrationEvents;
using TicketSalesPlatform.Payments.Api.Data;

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
                "Payment Service: Received Cancel Order {OrderId}. reason: {Reason}",
                message.OrderId,
                message.Reason
            );

            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p =>
                p.OrderId == message.OrderId
            );

            if (payment is null)
                return;

            try
            {
                payment.Cancel($"Order Cancelled: {message.Reason}");

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Payment CANCELLED for Order {OrderId}", message.OrderId);
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning(
                    "Conflict: Could not cancel payment for Order {OrderId}. It might be Completed already.",
                    message.OrderId
                );
            }
        }
    }
}
