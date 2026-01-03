using Marten;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Domain.Aggregates;

namespace TicketSalesPlatform.Orders.Infrastructure.Persistence
{
    public sealed class OrderRepository : IRepository<Order>
    {
        private readonly IDocumentSession _session;

        public OrderRepository(IDocumentSession session) => _session = session;

        public void Add(Order entity)
        {
            _session.Events.StartStream(entity.Id, entity.GetDomainEvents());
            entity.ClearDomainEvents();
        }

        public async Task<Order?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            return await _session.Events.AggregateStreamAsync<Order>(id, token: cancellationToken);
        }

        public void Update(Order entity)
        {
            var events = entity.GetDomainEvents();

            if (events != null && events.Any())
            {
                _session.Events.Append(entity.Id, events);
                entity.ClearDomainEvents();
            }
        }
    }
}
