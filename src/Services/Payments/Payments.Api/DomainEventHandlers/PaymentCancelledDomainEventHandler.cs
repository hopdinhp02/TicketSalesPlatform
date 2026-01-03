using MassTransit;
using MediatR;
using TicketSalesPlatform.IntegrationEvents;
using TicketSalesPlatform.Payments.Api.Entities.DomainEvents;

namespace TicketSalesPlatform.Payments.Api.DomainEventHandlers
{
    public class PaymentCancelledDomainEventHandler : INotificationHandler<PaymentCancelled>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<PaymentCancelledDomainEventHandler> _logger;

        public PaymentCancelledDomainEventHandler(
            IPublishEndpoint publishEndpoint,
            ILogger<PaymentCancelledDomainEventHandler> logger
        )
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Handle(PaymentCancelled notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Payment Domain Event received: Payment {PaymentId} Cancelled. Order {OrderId}. Reason: {Reason}. Publishing Integration Event...",
                notification.PaymentId,
                notification.OrderId,
                notification.Reason
            );

            var integrationEvent = new PaymentCancelledIntegrationEvent(
                notification.PaymentId,
                notification.OrderId,
                notification.Reason,
                DateTime.UtcNow
            );

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "Successfully published PaymentCancelledIntegrationEvent for Payment {PaymentId} / Order {OrderId}.",
                notification.PaymentId,
                notification.OrderId
            );
        }
    }
}
