using MassTransit;
using MediatR;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Payments.Api.Entities.DomainEvents;

namespace TicketSalesPlatform.Payments.Api.DomainEventHandlers
{
    public class PaymentCompletedDomainEventHandler : INotificationHandler<PaymentCompleted>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<PaymentCompletedDomainEventHandler> _logger;

        public PaymentCompletedDomainEventHandler(
            IPublishEndpoint publishEndpoint,
            ILogger<PaymentCompletedDomainEventHandler> logger
        )
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Handle(PaymentCompleted notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Domain Event: Payment {PaymentId} completed. Publishing integration event...",
                notification.Id
            );

            var integrationEvent = new OrderPaymentSucceededIntegrationEvent(
                notification.OrderId,
                notification.Id,
                DateTime.UtcNow
            );

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "--> Sent OrderPaymentSucceededIntegrationEvent for Order {OrderId}",
                notification.OrderId
            );
        }
    }
}
