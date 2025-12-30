using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.IntegrationEvents;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Entities;

namespace TicketSalesPlatform.Inventory.Api.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlacedIntegrationEvent>
{
    private readonly ILogger<OrderPlacedConsumer> _logger;
    private readonly InventoryDbContext _dbContext;

    public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger, InventoryDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<OrderPlacedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Inventory Service: Received Order {OrderId}. Allocating seats...",
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
                    "OUT OF STOCK: Order {OrderId} requested {Qty} seats of type {Type}, but only {Found} available.",
                    message.OrderId,
                    item.Quantity,
                    item.TicketTypeId,
                    seatsToReserve.Count
                );

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

        _logger.LogInformation(
            "Success! Reserved {Count} seats for Order {OrderId}. Waiting for payment.",
            message.Items.Sum(x => x.Quantity),
            message.OrderId
        );
    }
}
