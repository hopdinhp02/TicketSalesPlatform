using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.IntegrationEvents;
using TicketSalesPlatform.Inventory.Api.Data;

namespace TicketSalesPlatform.Inventory.Api.Consumers
{
    public class PaymentCancelledConsumer : IConsumer<PaymentCancelledIntegrationEvent>
    {
        private readonly InventoryDbContext _dbContext;
        private readonly ILogger<PaymentCancelledConsumer> _logger;

        public PaymentCancelledConsumer(
            InventoryDbContext dbContext,
            ILogger<PaymentCancelledConsumer> logger
        )
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentCancelledIntegrationEvent> context)
        {
            var message = context.Message;

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var seats = await _dbContext
                    .Seats.Where(s => s.OrderId == message.OrderId)
                    .ToListAsync();

                if (!seats.Any())
                {
                    _logger.LogWarning(
                        "Inventory: No seats found for Order {OrderId} to cancel. Ignoring.",
                        message.OrderId
                    );
                    return;
                }

                foreach (var seat in seats)
                {
                    seat.Cancel();

                    _logger.LogDebug(
                        "Inventory: Marking Seat {SeatId} as Available (Cancelled) pending commit.",
                        seat.Id
                    );
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Inventory: Successfully CANCELLED reservation for {Count} seats of Order {OrderId}.",
                    seats.Count,
                    message.OrderId
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Inventory: FAILED to cancel seats for Order {OrderId}. Rolling back transaction.",
                    message.OrderId
                );
                throw;
            }
        }
    }
}
