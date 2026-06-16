using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TicketSalesPlatform.Contracts.Commands;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Entities;

public class ReserveStockConsumer : IConsumer<ReserveStockCommand>
{
    private readonly ILogger<ReserveStockConsumer> _logger;
    private readonly InventoryDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;

    public ReserveStockConsumer(ILogger<ReserveStockConsumer> logger, InventoryDbContext dbContext, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _dbContext = dbContext;
        _redis = redis;
    }

    public async Task Consume(ConsumeContext<ReserveStockCommand> context)
    {
        var message = context.Message;
        var redisDb = _redis.GetDatabase();

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

        var itemsToRollback = message.Items?.Select(item => (item.TicketTypeId, item.Quantity)).ToList() ?? new List<(Guid, int)>();

        try
        {
            _logger.LogInformation(
                "Inventory: Allocating seats for Order {OrderId} in PostgreSQL...",
                message.OrderId
            );

            if (message.Items == null || !message.Items.Any())
            {
                _logger.LogWarning("Order {OrderId} has no items. Skipping.", message.OrderId);
                return;
            }

            // 1. POSTGRES BULK UPDATE
            foreach (var item in message.Items)
            {
                var rowsUpdated = await _dbContext.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""Seats"" 
                      SET ""Status"" = 1, ""UserId"" = {0}, ""OrderId"" = {1}, ""ReservationExpiresAt"" = {2}
                      WHERE ""Id"" IN (
                          SELECT ""Id"" FROM ""Seats""
                          WHERE ""TicketTypeId"" = {3} AND ""Status"" = {4}
                          ORDER BY ""SeatNo""
                          LIMIT {5}
                          FOR UPDATE SKIP LOCKED
                      )",
                    message.CustomerId,
                    message.OrderId,
                    DateTime.UtcNow.AddMinutes(15),
                    item.TicketTypeId,
                    (int)SeatStatus.Available,
                    item.Quantity
                );

                if (rowsUpdated < item.Quantity)
                {
                    throw new InvalidOperationException($"Postgres seat assignment mismatch. Expected: {item.Quantity}, Updated: {rowsUpdated}");
                }
            }

            await context.Publish(
                new StockReservedIntegrationEvent(message.OrderId, DateTime.UtcNow)
            );

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Success! Reserved seats for Order {OrderId}.", message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for Order {OrderId}", message.OrderId);
            
            // Rollback Redis decrements on exception
            foreach (var item in itemsToRollback)
            {
                await redisDb.StringIncrementAsync($"inventory:tickettype:{item.TicketTypeId}:available", item.Quantity);
            }
            
            throw;
        }
    }
}
