using MassTransit;
using MediatR;
using TicketSalesPlatform.IntegrationEvents;
using TicketSalesPlatform.Payments.Api.Entities.DomainEvents;

namespace TicketSalesPlatform.Payments.Api.DomainEventHandlers
{
    public class PaymentRefundedDomainEventHandler : INotificationHandler<PaymentRefunded>
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public PaymentRefundedDomainEventHandler(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task Handle(PaymentRefunded notification, CancellationToken cancellationToken)
        {
            await _publishEndpoint.Publish(
                new PaymentRefundedIntegrationEvent(
                    notification.PaymentId,
                    notification.OrderId,
                    notification.OccurredOn
                ),
                cancellationToken
            );
        }
    }
}
