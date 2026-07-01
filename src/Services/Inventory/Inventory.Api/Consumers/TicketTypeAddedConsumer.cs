using MassTransit;
using StackExchange.Redis;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Entities;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace TicketSalesPlatform.Inventory.Api.Consumers
{
    public class TicketTypeAddedConsumer : IConsumer<TicketTypeAddedIntegrationEvent>
    {
        private readonly InventoryDbContext _dbContext;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<TicketTypeAddedConsumer> _logger;

        public TicketTypeAddedConsumer(
            InventoryDbContext dbContext,
            IConnectionMultiplexer redis,
            ILogger<TicketTypeAddedConsumer> logger
        )
        {
            _dbContext = dbContext;
            _redis = redis;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TicketTypeAddedIntegrationEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation(
                "Creating {Quantity} seats for TicketType {Name} using PostgreSQL binary COPY...",
                msg.Quantity,
                msg.Name
            );

            bool seatsExist = await _dbContext.Seats.AnyAsync(s => s.TicketTypeId == msg.TicketTypeId);
            if (seatsExist)
            {
                _logger.LogInformation(
                    "Seats for TicketType {TicketTypeId} ({Name}) already exist. Skipping database insert.",
                    msg.TicketTypeId,
                    msg.Name
                );
            }
            else
            {
                var connectionString = _dbContext.Database.GetConnectionString();
                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                using (var writer = await conn.BeginBinaryImportAsync(
                    "COPY \"Seats\" (\"Id\", \"SeatNo\", \"EventId\", \"TicketTypeId\", \"Status\", \"UserId\", \"OrderId\", \"ReservationExpiresAt\") FROM STDIN (FORMAT BINARY)"
                ))
                {
                    for (int i = 1; i <= msg.Quantity; i++)
                    {
                        await writer.StartRowAsync();
                        await writer.WriteAsync(Guid.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid);
                        await writer.WriteAsync($"{msg.Name}-{i}", NpgsqlTypes.NpgsqlDbType.Text);
                        await writer.WriteAsync(msg.EventId, NpgsqlTypes.NpgsqlDbType.Uuid);
                        await writer.WriteAsync(msg.TicketTypeId, NpgsqlTypes.NpgsqlDbType.Uuid);
                        await writer.WriteAsync((int)SeatStatus.Available, NpgsqlTypes.NpgsqlDbType.Integer);
                        await writer.WriteNullAsync();
                        await writer.WriteNullAsync();
                        await writer.WriteNullAsync();
                    }

                    await writer.CompleteAsync();
                }

                _logger.LogInformation("Created {Count} seats in PostgreSQL database.", msg.Quantity);
            }

            var redisDb = _redis.GetDatabase();
            await redisDb.StringSetAsync($"inventory:tickettype:{msg.TicketTypeId}:available", msg.Quantity);

            _logger.LogInformation("Successfully seeded Redis counter with {Count} available stock.", msg.Quantity);
        }
    }
}
