using Marten;
using TicketSalesPlatform.Events.Application.Abstractions;
using TicketSalesPlatform.Events.Domain.Aggregates;

namespace TicketSalesPlatform.Events.Infrastructure.Persistence
{
    public sealed class EventRepository : IRepository<Event>
    {
        private readonly IDocumentSession _session;

        public EventRepository(IDocumentSession session) => _session = session;

        public void Add(Event entity)
        {
            _session.Events.StartStream(entity.Id, entity.GetDomainEvents());
        }

        public async Task<Event?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            return await _session.Events.AggregateStreamAsync<Event>(id, token: cancellationToken);
        }
    }
}
