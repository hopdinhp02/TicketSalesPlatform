using Marten;
using TicketFlow.Orders.Application.Abstractions;

namespace TicketFlow.Orders.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly IDocumentSession _session;

        public UnitOfWork(IDocumentSession session) => _session = session;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _session.SaveChangesAsync(cancellationToken);
            return 0;
        }
    }
}
