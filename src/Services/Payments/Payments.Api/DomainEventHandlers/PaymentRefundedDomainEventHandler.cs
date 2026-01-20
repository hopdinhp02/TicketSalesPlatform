using MassTransit;
using MediatR;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Payments.Api.Entities.DomainEvents;

namespace TicketSalesPlatform.Payments.Api.DomainEventHandlers
{
    public class PaymentRefundedDomainEventHandler : INotificationHandler<PaymentRefunded>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<PaymentRefundedDomainEventHandler> _logger;

        public PaymentRefundedDomainEventHandler(
            IPublishEndpoint publishEndpoint,
            ILogger<PaymentRefundedDomainEventHandler> logger
        )
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Handle(PaymentRefunded notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Payment Domain Event received: Payment {PaymentId} REFUNDED. Order {OrderId}. Publishing Integration Event...",
                notification.PaymentId,
                notification.OrderId
            );

            await _publishEndpoint.Publish(
                new PaymentRefundedIntegrationEvent(
                    notification.PaymentId,
                    notification.OrderId,
                    notification.OccurredOn
                ),
                cancellationToken
            );

            _logger.LogInformation(
                "Successfully published PaymentRefundedIntegrationEvent for Payment {PaymentId} / Order {OrderId}.",
                notification.PaymentId,
                notification.OrderId
            );
        }
    }
}
