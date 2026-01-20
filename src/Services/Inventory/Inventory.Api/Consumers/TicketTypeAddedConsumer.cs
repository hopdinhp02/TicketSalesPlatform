using MassTransit;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Entities;

namespace TicketSalesPlatform.Inventory.Api.Consumers
{
    public class TicketTypeAddedConsumer : IConsumer<TicketTypeAddedIntegrationEvent>
    {
        private readonly InventoryDbContext _dbContext;
        private readonly ILogger<TicketTypeAddedConsumer> _logger;

        public TicketTypeAddedConsumer(
            InventoryDbContext dbContext,
            ILogger<TicketTypeAddedConsumer> logger
        )
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TicketTypeAddedIntegrationEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation(
                "Creating {Quantity} seats for TicketType {Name}...",
                msg.Quantity,
                msg.Name
            );

            var seats = new List<Seat>();

            for (int i = 1; i <= msg.Quantity; i++)
            {
                seats.Add(new Seat($"{msg.Name}-{i}", msg.EventId, msg.TicketTypeId));
            }

            await _dbContext.Seats.AddRangeAsync(seats);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created {Count} seats in Inventory.", seats.Count);
        }
    }
}
