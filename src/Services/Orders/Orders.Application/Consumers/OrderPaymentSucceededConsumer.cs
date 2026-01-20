using MassTransit;
using Microsoft.Extensions.Logging;
using TicketSalesPlatform.Contracts.Events;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Domain.Aggregates;

namespace TicketSalesPlatform.Orders.Application.Consumers
{
    public class OrderPaymentSucceededConsumer : IConsumer<OrderPaymentSucceededIntegrationEvent>
    {
        private readonly ILogger<OrderPaymentSucceededConsumer> _logger;
        private readonly IRepository<Order> _orderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OrderPaymentSucceededConsumer(
            ILogger<OrderPaymentSucceededConsumer> logger,
            IRepository<Order> orderRepository,
            IUnitOfWork unitOfWork
        )
        {
            _logger = logger;
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<OrderPaymentSucceededIntegrationEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation(
                "Order Service: Payment confirmed for Order {OrderId}. Updating status...",
                message.OrderId
            );

            var order = await _orderRepository.GetByIdAsync(message.OrderId);

            if (order is null)
            {
                _logger.LogError(
                    "Critical: Order {OrderId} paid but not found in DB.",
                    message.OrderId
                );
                return;
            }

            order.MarkAsPaid();

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Success: Order {OrderId} is now MARKED AS PAID.",
                message.OrderId
            );
        }
    }
}
