using MassTransit;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Domain.Aggregates;
using TicketSalesPlatform.Orders.Domain.Enums;

namespace TicketSalesPlatform.Orders.Application.Consumers
{
    public class OrderReservationFailedConsumer : IConsumer<OrderReservationFailedIntegrationEvent>
    {
        private readonly IRepository<Order> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderReservationFailedConsumer> _logger;

        public OrderReservationFailedConsumer(
            IRepository<Order> repository,
            IUnitOfWork unitOfWork,
            ILogger<OrderReservationFailedConsumer> logger
        )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderReservationFailedIntegrationEvent> context)
        {
            var message = context.Message;
            _logger.LogWarning(
                "Orders: Received Reservation FAILED for Order {OrderId}. Reason: {Reason}",
                message.OrderId,
                message.Reason
            );

            var order = await _repository.GetByIdAsync(message.OrderId);

            if (order is null)
            {
                _logger.LogError(
                    "Orders: Order {OrderId} not found while processing Reservation Failure!",
                    message.OrderId
                );
                return;
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                _logger.LogInformation(
                    "Orders: Order {OrderId} is already Cancelled. Skipping.",
                    message.OrderId
                );
                return;
            }

            order.MarkAsCancelled($"Inventory Failed: {message.Reason}");

            _repository.Update(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Orders: Order {OrderId} marked as CANCELLED due to inventory failure.",
                message.OrderId
            );

            await context.Publish(new OrderCancelledIntegrationEvent(order.Id, message.Reason));

            _logger.LogInformation(
                "Orders: Propagated OrderCancelled event for {OrderId}.",
                message.OrderId
            );
        }
    }
}
