using Marten;
using JasperFx.Events;
using Marten.Events.Projections;
using TicketSalesPlatform.Events.Domain.DomainEvents;
using TicketSalesPlatform.Events.Domain.ReadModels;

namespace TicketSalesPlatform.Events.Infrastructure.Projections
{
    public class TicketTypeProjection : IProjection
    {
        public void Apply(IDocumentOperations operations, IReadOnlyList<IEvent> events)
        {
        }

        public async Task ApplyAsync(IDocumentOperations operations, IReadOnlyList<IEvent> events, CancellationToken cancellation)
        {
            foreach (var @event in events)
            {
                if (@event.Data is TicketTypeAdded ticketTypeAdded)
                {
                    var view = new TicketTypeView
                    {
                        Id = ticketTypeAdded.TicketTypeId,
                        EventId = ticketTypeAdded.EventId,
                        Name = ticketTypeAdded.Name,
                        Price = ticketTypeAdded.Price,
                        Quantity = ticketTypeAdded.Quantity,
                        IsPublished = false
                    };
                    operations.Store(view);
                }
                else if (@event.Data is EventPublished eventPublished)
                {
                    // Find all TicketTypeViews for this EventId and set IsPublished to true
                    var ticketTypes = await operations.Query<TicketTypeView>()
                        .Where(t => t.EventId == eventPublished.EventId)
                        .ToListAsync(cancellation);
                        
                    foreach (var ticketType in ticketTypes)
                    {
                        ticketType.IsPublished = true;
                        operations.Store(ticketType);
                    }
                }
            }
        }
    }
}
