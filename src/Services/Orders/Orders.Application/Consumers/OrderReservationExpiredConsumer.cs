using MassTransit;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Domain.Aggregates;
using TicketSalesPlatform.Orders.Domain.Enums;

namespace TicketSalesPlatform.Orders.Application.Consumers
{
    public class OrderReservationExpiredConsumer
        : IConsumer<OrderReservationExpiredIntegrationEvent>
    {
        private readonly IRepository<Order> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderReservationExpiredConsumer> _logger;

        public OrderReservationExpiredConsumer(
            IRepository<Order> repository,
            IUnitOfWork unitOfWork,
            ILogger<OrderReservationExpiredConsumer> logger
        )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderReservationExpiredIntegrationEvent> context)
        {
            var orderId = context.Message.OrderId;
            var order = await _repository.GetByIdAsync(orderId);

            if (order is null)
                return;

            // IDEMPOTENCY CHECK
            if (order.Status == OrderStatus.Cancelled)
            {
                _logger.LogInformation(
                    "Order {OrderId} is already Cancelled. Skipping expiration message.",
                    orderId
                );
                return;
            }

            // 2. RACE CONDITION: PAID vs EXPIRED
            if (order.Status == OrderStatus.Paid)
            {
                _logger.LogWarning(
                    "Race Condition Ignore: Order {OrderId} is PAID but received Expiration message. "
                        + "Assuming Inventory Service handles the Seat Re-claim or Refund triggering.",
                    orderId
                );
                return;
            }

            order.MarkAsCancelled("Reservation Expired");

            _repository.Update(order);
            await _unitOfWork.SaveChangesAsync();

            await context.Publish(
                new OrderCancelledIntegrationEvent(order.Id, "Reservation Expired")
            );

            _logger.LogInformation(
                "Order {OrderId} cancelled due to timeout. Notification sent to Payment Service.",
                orderId
            );
        }
    }
}
