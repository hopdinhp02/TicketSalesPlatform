using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Contracts.Commands;
using TicketSalesPlatform.Payments.Api.Data;

namespace TicketSalesPlatform.Payments.Api.Consumers
{
    public class RefundPaymentConsumer : IConsumer<RefundPaymentCommand>
    {
        private readonly ILogger<RefundPaymentConsumer> _logger;
        private readonly PaymentDbContext _dbContext;
        private readonly IPublisher _publisher;

        public RefundPaymentConsumer(
            ILogger<RefundPaymentConsumer> logger,
            PaymentDbContext dbContext,
            IPublisher publisher
        )
        {
            _logger = logger;
            _dbContext = dbContext;
            _publisher = publisher;
        }

        public async Task Consume(ConsumeContext<RefundPaymentCommand> context)
        {
            var message = context.Message;
            _logger.LogWarning(
                "Processing REFUND request for Order {OrderId}. Reason: {Reason}",
                message.OrderId,
                message.Reason
            );

            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p =>
                p.OrderId == message.OrderId
            );

            if (payment is null)
            {
                _logger.LogError(
                    "Refund failed: Payment not found for Order {OrderId}",
                    message.OrderId
                );
                return;
            }

            // Mocking external payment gateway
            bool gatewayRefundSuccess = true;

            if (gatewayRefundSuccess)
            {
                try
                {
                    payment.Refund(message.Reason);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation(
                        "Successfully REFUNDED Payment {PaymentId} for Order {OrderId}",
                        payment.Id,
                        message.OrderId
                    );
                    foreach (var domainEvent in payment.GetDomainEvents())
                    {
                        await _publisher.Publish(domainEvent);
                    }
                    payment.ClearDomainEvents();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating payment status to Refunded.");
                    throw;
                }
            }
            else
            {
                _logger.LogCritical(
                    "Gateway refused refund for Order {OrderId}. Manual intervention required!",
                    message.OrderId
                );
            }
        }
    }
}
