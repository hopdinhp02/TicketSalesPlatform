using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Entities;

namespace TicketSalesPlatform.Inventory.Api.Jobs
{
    public class ExpiredReservationCleanupService : BackgroundService
    {
        private readonly ILogger<ExpiredReservationCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ExpiredReservationCleanupService(
            ILogger<ExpiredReservationCleanupService> logger,
            IServiceProvider serviceProvider
        )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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

                _logger.LogInformation(
                    "Expired reservation for Order {OrderId}. Released {SeatCount} seats.",
                    orderId,
                    seats.Count
                );
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
