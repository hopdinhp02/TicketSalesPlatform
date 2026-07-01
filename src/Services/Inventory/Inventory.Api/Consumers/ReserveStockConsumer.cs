using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

    public ReserveStockConsumer(
        ILogger<ReserveStockConsumer> logger,
        InventoryDbContext dbContext,
        IConnectionMultiplexer redis
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _redis = redis;
    }

    public async Task Consume(ConsumeContext<ReserveStockCommand> context)
    {
        var message = context.Message;
        var redisDb = _redis.GetDatabase();

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

            foreach (var item in message.Items)
            {
                var rowsUpdated = await _dbContext.Database.ExecuteSqlRawAsync(
                    @"WITH sel AS (
                          SELECT ""Id"" FROM ""Seats""
                          WHERE ""TicketTypeId"" = @ticketTypeId AND ""Status"" = @availableStatus
                          ORDER BY ""SeatNo""
                          LIMIT @quantity
                          FOR UPDATE SKIP LOCKED
                      )
                      UPDATE ""Seats"" 
                      SET ""Status"" = @reservedStatus, ""UserId"" = @userId, ""OrderId"" = @orderId, ""ReservationExpiresAt"" = @expiresAt
                      FROM sel
                      WHERE ""Seats"".""Id"" = sel.""Id""",
                    new NpgsqlParameter("@userId", (object)message.CustomerId),
                    new NpgsqlParameter("@orderId", (object)message.OrderId),
                    new NpgsqlParameter("@expiresAt", (object)DateTime.UtcNow.AddMinutes(15)),
                    new NpgsqlParameter("@ticketTypeId", (object)item.TicketTypeId),
                    new NpgsqlParameter("@availableStatus", (object)(int)SeatStatus.Available),
                    new NpgsqlParameter("@quantity", (object)item.Quantity),
                    new NpgsqlParameter("@reservedStatus", (object)(int)SeatStatus.Reserved)
                );

                if (rowsUpdated < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Postgres seat assignment mismatch. Expected: {item.Quantity}, Updated: {rowsUpdated}"
                    );
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
            _logger.LogError(
                ex,
                "Error reserving stock for Order {OrderId}. Rethrowing for retry/fault handling.",
                message.OrderId
            );
            throw;
        }
    }
}
