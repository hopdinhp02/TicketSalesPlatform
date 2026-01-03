using Marten.Events.Aggregation;
using TicketSalesPlatform.Events.Domain.DomainEvents;
using TicketSalesPlatform.Events.Domain.ReadModels;

namespace TicketSalesPlatform.Events.Infrastructure.Projections
{
    public class EventDetailProjection : SingleStreamProjection<EventDetailView, Guid>
    {
        public EventDetailView Create(EventCreated e) =>
            new EventDetailView
            {
                Id = e.EventId,
                Title = e.Title,
                IsPublished = false,
                TicketTypes = new List<TicketTypeDto>(),
            };

        public void Apply(TicketTypeAdded e, EventDetailView view)
        {
            view.TicketTypes.Add(
                new TicketTypeDto(e.TicketTypeId, e.EventId, e.Name, e.Price, e.Quantity)
            );
        }

        public void Apply(EventPublished e, EventDetailView view)
        {
            view.IsPublished = true;
        }
    }
}
