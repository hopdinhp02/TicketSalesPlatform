using MediatR;
using Orders.Domain.Aggregates;
using TicketFlow.Orders.Application.Abstractions;
using TicketFlow.Orders.Domain.Aggregates;

namespace TicketFlow.Orders.Application.PlaceOrder
{
    public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Guid>
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;

        public PlaceOrderCommandHandler(
            IRepository<Order> orderRepository,
            IUnitOfWork unitOfWork,
            IPublisher publisher
        )
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _publisher = publisher;
        }

        public async Task<Guid> Handle(
            PlaceOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            var orderItems = request
                .Items.Select(item => new OrderItem(
                    Guid.Empty,
                    item.TicketTypeId,
                    item.EventName,
                    item.UnitPrice,
                    item.Quantity
                ))
                .ToList();
            var order = Order.Create(request.CustomerId, orderItems);
            _orderRepository.Add(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            foreach (var domainEvent in order.GetDomainEvents())
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
            order.ClearDomainEvents();
            return order.Id;
        }
    }
}
