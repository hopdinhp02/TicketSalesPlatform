using Marten;
using TicketSalesPlatform.Events.Application.Abstractions;

namespace TicketSalesPlatform.Events.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly IDocumentSession _session;

        public UnitOfWork(IDocumentSession session) => _session = session;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _session.SaveChangesAsync(cancellationToken);
            return 0; // Marten doesn't return a count of changed entities, so return 0.
        }
    }
}
