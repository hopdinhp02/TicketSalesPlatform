using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Events.Domain.DomainEvents;

namespace TicketSalesPlatform.Events.Application.DomainEventHandlers
{
    public class TicketTypeAddedDomainEventHandler : INotificationHandler<TicketTypeAdded>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<TicketTypeAddedDomainEventHandler> _logger;

        public TicketTypeAddedDomainEventHandler(
            IPublishEndpoint publishEndpoint,
            ILogger<TicketTypeAddedDomainEventHandler> logger
        )
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Handle(TicketTypeAdded notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Publishing TicketTypeAddedIntegrationEvent for {Name}",
                notification.Name
            );

            await _publishEndpoint.Publish(
                new TicketTypeAddedIntegrationEvent(
                    notification.EventId,
                    notification.TicketTypeId,
                    notification.Name,
                    notification.Price,
                    notification.Quantity
                ),
                cancellationToken
            );

            _logger.LogInformation(
                "--> Sent TicketTypeAddedIntegrationEvent for {Name}",
                notification.Name
            );
        }
    }
}
