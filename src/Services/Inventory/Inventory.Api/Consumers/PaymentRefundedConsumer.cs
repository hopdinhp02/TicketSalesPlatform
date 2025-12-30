using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.IntegrationEvents;
using TicketSalesPlatform.Inventory.Api.Data;

namespace TicketSalesPlatform.Inventory.Api.Consumers
{
    public class PaymentRefundedConsumer : IConsumer<PaymentRefundedIntegrationEvent>
    {
        private readonly InventoryDbContext _dbContext;
        private readonly ILogger<PaymentRefundedConsumer> _logger;

        public PaymentRefundedConsumer(
            InventoryDbContext dbContext,
            ILogger<PaymentRefundedConsumer> logger
        )
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentRefundedIntegrationEvent> context)
        {
            var message = context.Message;

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var seats = await _dbContext
                    .Seats.Where(s => s.OrderId == message.OrderId)
                    .ToListAsync();

                if (!seats.Any())
                    return; // Idempotency

                foreach (var seat in seats)
                {
                    seat.Refund();

                    _logger.LogDebug("Marking Seat {SeatId} as Refunded pending commit.", seat.Id);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Inventory: Successfully released {Count} seats for Order {OrderId}.",
                    seats.Count,
                    message.OrderId
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "FAILED to refund seats for Order {OrderId}. Rolling back transaction.",
                    message.OrderId
                );
                throw;
            }
        }
    }
}
