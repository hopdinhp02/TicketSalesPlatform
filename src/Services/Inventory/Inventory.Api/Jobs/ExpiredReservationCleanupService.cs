using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Entities;

namespace TicketSalesPlatform.Inventory.Api.Jobs
{
    public class ExpiredReservationCleanupService : BackgroundService
    {
        private readonly ILogger<ExpiredReservationCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionMultiplexer _redis;

        public ExpiredReservationCleanupService(
            ILogger<ExpiredReservationCleanupService> logger,
            IServiceProvider serviceProvider,
            IConnectionMultiplexer redis
        )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _redis = redis;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredReservations(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired reservations");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task ProcessExpiredReservations(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var expiredOrderIds = await dbContext
                .Seats.Where(s =>
                    s.Status == SeatStatus.Reserved && s.ReservationExpiresAt < DateTime.UtcNow
                )
                .Select(s => s.OrderId)
                .Distinct()
                .ToListAsync(stoppingToken);

            if (expiredOrderIds.Count == 0)
                return;

            _logger.LogInformation(
                "Found {Count} orders with expired reservations.",
                expiredOrderIds.Count
            );

            var redisDb = _redis.GetDatabase();

            foreach (var orderId in expiredOrderIds)
            {
                if (orderId == null)
                    continue;

                var seats = await dbContext
                    .Seats.Where(s => s.OrderId == orderId && s.Status == SeatStatus.Reserved)
                    .ToListAsync(stoppingToken);

                foreach (var seat in seats)
                {
                    seat.Expire();
                }

                await publishEndpoint.Publish(
                    new OrderReservationExpiredIntegrationEvent(orderId.Value),
                    stoppingToken
                );

                // Increment Redis counter back for released seats
                var releasedGroups = seats.GroupBy(s => s.TicketTypeId);
                foreach (var group in releasedGroups)
                {
                    var redisKey = $"inventory:tickettype:{group.Key}:available";
                    await redisDb.StringIncrementAsync(redisKey, group.Count());
                }

                _logger.LogInformation(
                    "Expired reservation for Order {OrderId}. Released {SeatCount} seats and returned to Redis.",
                    orderId,
                    seats.Count
                );
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
