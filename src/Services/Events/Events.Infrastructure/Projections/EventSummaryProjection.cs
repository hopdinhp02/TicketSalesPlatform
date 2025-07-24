using Marten.Events.Aggregation;
using TicketFlow.Events.Domain.DomainEvents;
using TicketFlow.Events.Domain.ReadModels;

namespace TicketFlow.Events.Infrastructure.Projections
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
                Date = e.Date,
                IsPublished = false,
            };

        // This method is called for subsequent events in the stream.
        // It applies changes to the existing read model document.
        // NOTE: add an Apply method for an EventPublished event here.
        // public void Apply(EventPublished e, EventSummary current) => current.IsPublished = true;
    }
}
