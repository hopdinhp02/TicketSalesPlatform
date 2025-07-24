using MassTransit;
using MediatR;
using TicketFlow.IntegrationEvents;
using TicketFlow.Orders.Domain.DomainEvents;

namespace TicketFlow.Orders.Application.PlaceOrder
{
    public sealed class OrderPlacedDomainEventHandler : INotificationHandler<OrderPlaced>
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderPlacedDomainEventHandler(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task Handle(OrderPlaced domainEvent, CancellationToken cancellationToken)
        {
            // Create the integration event from the domain event.
            var integrationEvent = new OrderPlacedIntegrationEvent(
                domainEvent.OrderId,
                domainEvent.CustomerId,
                domainEvent.TotalPrice,
                domainEvent.OccurredOn
            );

            // Publish the event to the message bus (RabbitMQ).
            await _publishEndpoint.Publish(integrationEvent, cancellationToken);

            Console.WriteLine(
                $"--> Published OrderPlacedIntegrationEvent for OrderId: {integrationEvent.OrderId}"
            );
        }
    }
}
