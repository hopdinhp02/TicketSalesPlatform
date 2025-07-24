using Marten;
using Orders.Domain.Aggregates;
using TicketFlow.Orders.Application.Abstractions;

namespace TicketFlow.Orders.Infrastructure.Persistence
{
    public sealed class OrderRepository : IRepository<Order>
    {
        private readonly IDocumentSession _session;

        public OrderRepository(IDocumentSession session) => _session = session;

        public void Add(Order entity)
        {
            // For a new aggregate, we start a new event stream.
            _session.Events.StartStream(entity.Id, entity.GetDomainEvents());
        }

        public async Task<Order?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            // Marten will fetch all events for the given ID and replay them to reconstruct the aggregate.
            return await _session.Events.AggregateStreamAsync<Order>(id, token: cancellationToken);
        }

        // NOTE: An 'Update' method would use session.Events.Append(entity.Id, entity.GetDomainEvents());
    }
}
