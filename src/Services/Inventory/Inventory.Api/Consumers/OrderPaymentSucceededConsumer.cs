using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.IntegrationEvents;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Entities;

namespace TicketSalesPlatform.Inventory.Api.Consumers
{
    public class OrderPaymentSucceededConsumer : IConsumer<OrderPaymentSucceededIntegrationEvent>
    {
        private readonly ILogger<OrderPaymentSucceededConsumer> _logger;
        private readonly InventoryDbContext _dbContext;

        public OrderPaymentSucceededConsumer(
            ILogger<OrderPaymentSucceededConsumer> logger,
            InventoryDbContext dbContext
        )
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<OrderPaymentSucceededIntegrationEvent> context)
        {
            var message = context.Message;

            try
            {
                _logger.LogInformation(
                    "Inventory Service: Payment succeeded for Order {OrderId}. Finalizing ticket allocation...",
                    message.OrderId
                );

                var reservedSeats = await _dbContext
                    .Seats.Where(s =>
                        s.OrderId == message.OrderId && s.Status == SeatStatus.Reserved
                    )
                    .ToListAsync();

                if (reservedSeats.Count == 0)
                {
                    _logger.LogWarning(
                        "Warning: Order {OrderId} is paid but no reserved seats found. (Seats might have expired or released).",
                        message.OrderId
                    );
                    return;
                }

                foreach (var seat in reservedSeats)
                {
                    seat.MarkAsSold();
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Success! Marked {Count} seats as SOLD for Order {OrderId}.",
                    reservedSeats.Count,
                    message.OrderId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ERROR: Failed to finalize seats for Order {OrderId}.",
                    message.OrderId
                );

                throw;
            }
        }
    }
}
