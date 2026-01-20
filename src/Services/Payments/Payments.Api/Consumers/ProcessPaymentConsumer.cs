using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Contracts.Commands;
using TicketSalesPlatform.Payments.Api.Data;

namespace TicketSalesPlatform.Payments.Api.Consumers
{
    public class ProcessPaymentConsumer : IConsumer<ProcessPaymentCommand>
    {
        private readonly ILogger<ProcessPaymentConsumer> _logger;
        private readonly PaymentDbContext _dbContext;

        public ProcessPaymentConsumer(
            ILogger<ProcessPaymentConsumer> logger,
            PaymentDbContext dbContext
        )
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<ProcessPaymentCommand> context)
        {
            var message = context.Message;
            _logger.LogInformation(
                "Payment Service: Received Order {OrderId}. Creating pending invoice...",
                message.OrderId
            );

            // IDEMPOTENCY CHECK
            var exists = await _dbContext.Payments.AnyAsync(p => p.OrderId == message.OrderId);
            if (exists)
            {
                _logger.LogInformation(
                    "Payment already exists for Order {OrderId}. Skipping.",
                    message.OrderId
                );
                return;
            }

            try
            {
                var payment = new Entities.Payment(
                    message.OrderId,
                    message.CustomerId,
                    message.TotalPrice
                );

                _dbContext.Payments.Add(payment);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Pending Payment created. Id: {PaymentId}. Waiting for user to pay via API.",
                    payment.Id
                );
            }
            catch (DbUpdateException)
            {
                // RACE CONDITION
                _logger.LogInformation(
                    "Payment already created by API for Order {OrderId}. Consumer skipping.",
                    message.OrderId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create payment record for Order {OrderId}",
                    message.OrderId
                );
                throw; // Throw so RabbitMQ retries later
            }
        }
    }
}
