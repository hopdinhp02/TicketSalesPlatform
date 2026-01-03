using Marten.Events.Aggregation;
using TicketSalesPlatform.Events.Domain.DomainEvents;
using TicketSalesPlatform.Events.Domain.ReadModels;

namespace TicketSalesPlatform.Events.Infrastructure.Projections
{
    /// <summary>
    /// A Marten projection that builds the EventSummary read model.
    /// It listens to the event stream and updates the read model document accordingly.
    /// </summary>
    public class EventSummaryProjection : SingleStreamProjection<EventSummary, Guid>
    {
        public EventSummaryProjection() { }

        // This method is called when the first event for a stream arrives.
        // It creates the initial read model document.
        public EventSummary Create(EventCreated e) =>
            new()
            {
                Id = e.EventId,
                Title = e.Title,
                Description = e.Description,
                Date = e.Date,
                IsPublished = false,
            };

        public void Apply(EventPublished e, EventSummary current) => current.IsPublished = true;

        public void Apply(TicketTypeAdded e, EventSummary current)
        {
            current.TotalTickets += e.Quantity;

            if (e.Price < current.MinPrice)
                current.MinPrice = e.Price;
            if (e.Price > current.MaxPrice)
                current.MaxPrice = e.Price;
        }
    }
}
