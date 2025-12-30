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
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation(
                    "Inventory Service: Payment succeeded for Order {OrderId}. Finalizing ticket allocation...",
                    message.OrderId
                );

                var reservedSeats = await _dbContext
                    .Seats.Where(s => s.OrderId == message.OrderId)
                    .ToListAsync();

                if (reservedSeats.Count == 0)
                {
                    _logger.LogWarning(
                        "Warning: Order {OrderId} is paid but no reserved seats found. (Seats might have expired or released).",
                        message.OrderId
                    );
                    await InitiateRefund(context, "No seats found for paid order");
                    return;
                }

                foreach (var seat in reservedSeats)
                {
                    if (seat.Status == SeatStatus.Reserved)
                    {
                        seat.MarkAsSold();
                    }
                    else if (seat.Status == SeatStatus.Available)
                    {
                        _logger.LogWarning(
                            "Seat {SeatId} was released but Payment succeeded. Re-claiming it!",
                            seat.Id
                        );
                        seat.MarkAsSold();
                    }
                    else if (seat.Status == SeatStatus.Sold && seat.OrderId != message.OrderId)
                    {
                        _logger.LogCritical(
                            "Conflict! Seat {SeatId} sold to another user. Initiating REFUND.",
                            seat.Id
                        );
                        await InitiateRefund(context, "Seat sold to another user");
                        return;
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

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
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task InitiateRefund(
            ConsumeContext<OrderPaymentSucceededIntegrationEvent> context,
            string reason
        )
        {
            var orderId = context.Message.OrderId;

            _logger.LogError(
                "CRITICAL: Initiating REFUND for Order {OrderId}. Reason: {Reason}",
                orderId,
                reason
            );

            await context.Publish(new RefundRequiredIntegrationEvent(orderId, reason));
        }
    }
}
