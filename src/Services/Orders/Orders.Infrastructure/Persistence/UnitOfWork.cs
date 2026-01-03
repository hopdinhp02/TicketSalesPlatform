using Marten;
using MediatR;
using TicketSalesPlatform.Orders.Application.Abstractions;

namespace TicketSalesPlatform.Orders.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly IDocumentSession _session;
        private readonly IPublisher _publisher;

        public UnitOfWork(IDocumentSession session, IPublisher publisher)
        {
            _session = session;
            _publisher = publisher;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var pendingEvents = _session
                .PendingChanges.Streams()
                .SelectMany(s => s.Events)
                .Select(e => e.Data)
                .ToList();

            foreach (var @event in pendingEvents)
            {
                await _publisher.Publish(@event, cancellationToken);
            }

            await _session.SaveChangesAsync(cancellationToken);
            return 0;
        }
    }
}
