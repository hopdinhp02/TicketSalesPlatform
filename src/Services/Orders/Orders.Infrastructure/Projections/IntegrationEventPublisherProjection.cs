using Marten;
using JasperFx.Events;
using Marten.Events.Projections;
using MassTransit;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.Contracts.Dtos;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Orders.Domain.DomainEvents;

namespace TicketSalesPlatform.Orders.Infrastructure.Projections
{
    public class IntegrationEventPublisherProjection : IProjection
    {
        private readonly IBus _bus;
        private readonly ILogger<IntegrationEventPublisherProjection> _logger;

        public IntegrationEventPublisherProjection(IBus bus, ILogger<IntegrationEventPublisherProjection> logger)
        {
            _bus = bus;
            _logger = logger;
        }

        public void Apply(IDocumentOperations operations, IReadOnlyList<IEvent> events)
        {
            // Sync version - no-op as event publishing is entirely asynchronous
        }

        public async Task ApplyAsync(IDocumentOperations operations, IReadOnlyList<IEvent> events, CancellationToken cancellation)
        {
            foreach (var @event in events)
            {
                if (@event.Data is OrderPlaced orderPlaced)
                {
                    var itemsDto = orderPlaced.Items
                        .Select(i => new OrderItemDto(i.TicketTypeId, i.Quantity))
                        .ToList();

                    var integrationEvent = new OrderPlacedIntegrationEvent(
                        orderPlaced.OrderId,
                        orderPlaced.CustomerId,
                        orderPlaced.TotalPrice,
                        orderPlaced.OccurredOn,
                        itemsDto
                    );

                    await _bus.Publish(integrationEvent, cancellation);

                    _logger.LogInformation(
                        "--> [Outbox] Published OrderPlacedIntegrationEvent for OrderId: {OrderId} with {Count} items.",
                        integrationEvent.OrderId,
                        itemsDto.Count
                    );
                }
            }
        }
    }
}
