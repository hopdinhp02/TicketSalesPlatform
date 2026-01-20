using MassTransit;
using MediatR;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Payments.Api.Entities.DomainEvents;

namespace TicketSalesPlatform.Payments.Api.DomainEventHandlers
{
    public class PaymentFailedDomainEventHandler : INotificationHandler<PaymentFailed>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<PaymentFailedDomainEventHandler> _logger;

        public PaymentFailedDomainEventHandler(
            IPublishEndpoint publishEndpoint,
            ILogger<PaymentFailedDomainEventHandler> logger
        )
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Handle(PaymentFailed notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Payment Domain Event received: Payment {PaymentId} Failed. Order {OrderId}. Reason: {Reason}. Publishing Integration Event...",
                notification.PaymentId,
                notification.OrderId,
                notification.Reason
            );

            var integrationEvent = new PaymentFailedIntegrationEvent(
                notification.PaymentId,
                notification.OrderId,
                notification.Reason,
                DateTime.UtcNow
            );

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "Successfully published PaymentFailedIntegrationEvent for Payment {PaymentId} / Order {OrderId}.",
                notification.PaymentId,
                notification.OrderId
            );
        }
    }
}
