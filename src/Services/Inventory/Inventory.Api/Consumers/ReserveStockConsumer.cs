using System.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Contracts.Commands;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Entities;

public class ReserveStockConsumer : IConsumer<ReserveStockCommand>
{
    private readonly ILogger<ReserveStockConsumer> _logger;
    private readonly InventoryDbContext _dbContext;

    public ReserveStockConsumer(ILogger<ReserveStockConsumer> logger, InventoryDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<ReserveStockCommand> context)
    {
        var message = context.Message;

        // IDEMPOTENCY CHECK
        bool alreadyReserved = await _dbContext.Seats.AnyAsync(s => s.OrderId == message.OrderId);
        if (alreadyReserved)
        {
            _logger.LogInformation(
                "Order {OrderId} already reserved seats. Skipping logic.",
                message.OrderId
            );
            await context.Publish(
                new StockReservedIntegrationEvent(message.OrderId, DateTime.UtcNow)
            );
            return;
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable
        );

        try
        {
            _logger.LogInformation(
                "Inventory: Allocating seats for Order {OrderId}...",
                message.OrderId
            );

            if (message.Items == null || !message.Items.Any())
            {
                _logger.LogWarning("Order {OrderId} has no items. Skipping.", message.OrderId);
                return;
            }

            foreach (var item in message.Items)
            {
                var seatsToReserve = await _dbContext
                    .Seats.Where(s =>
                        s.TicketTypeId == item.TicketTypeId && s.Status == SeatStatus.Available
                    )
                    .OrderBy(s => s.SeatNo)
                    .Take(item.Quantity)
                    .ToListAsync();

                if (seatsToReserve.Count < item.Quantity)
                {
                    _logger.LogError(
                        "OUT OF STOCK: Order {OrderId} requested {Qty}, found {Found}.",
                        message.OrderId,
                        item.Quantity,
                        seatsToReserve.Count
                    );

                    await transaction.RollbackAsync();

                    await context.Publish(
                        new OrderReservationFailedIntegrationEvent(
                            message.OrderId,
                            $"Out of stock for TicketType {item.TicketTypeId}"
                        )
                    );
                    return;
                }

                foreach (var seat in seatsToReserve)
                {
                    seat.Reserve(message.CustomerId, message.OrderId);
                }
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Success! Reserved seats for Order {OrderId}.", message.OrderId);

            await context.Publish(
                new StockReservedIntegrationEvent(message.OrderId, DateTime.UtcNow)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for Order {OrderId}", message.OrderId);
            await transaction.RollbackAsync();

            throw;
        }
    }
}
