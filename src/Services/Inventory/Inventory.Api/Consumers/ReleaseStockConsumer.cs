using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Contracts.Commands;
using TicketSalesPlatform.Inventory.Api.Data;

namespace TicketSalesPlatform.Inventory.Api.Consumers
{
    public class ReleaseStockConsumer : IConsumer<ReleaseStockCommand>
    {
        private readonly InventoryDbContext _dbContext;
        private readonly ILogger<ReleaseStockConsumer> _logger;

        public ReleaseStockConsumer(
            InventoryDbContext dbContext,
            ILogger<ReleaseStockConsumer> logger
        )
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ReleaseStockCommand> context)
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
