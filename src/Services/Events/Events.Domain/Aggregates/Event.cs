using SharedKernel;
using TicketFlow.Events.Domain.DomainEvents;
using NewId = MassTransit.NewId;

namespace TicketFlow.Events.Domain.Aggregates
{
    public sealed class Event : AggregateRoot<Guid>
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public DateTime Date { get; private set; }
        public bool IsPublished { get; private set; }

        private Event(Guid id, string title, string description, DateTime date)
            : base(id)
        {
            Title = title;
            Description = description;
            Date = date;
            IsPublished = false; // An event always starts as a draft.
        }

        /// <summary>
        /// Factory method for creating a new Event.
        /// This is the only public way to create an Event instance, ensuring all
        /// business rules for creation are met.
        /// </summary>
        public static Event Create(string title, string description, DateTime date)
        {
            // Enforce Invariants (Business Rules)
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be empty.", nameof(title));
            }

            if (date < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Cannot create an event in the past.");
            }

            var newEvent = new Event(NewId.NextGuid(), title, description, date);

            newEvent.AddDomainEvent(new EventCreated(newEvent.Id, newEvent.Title, newEvent.Date));

            return newEvent;
        }

        /// <summary>
        /// Business method to publish an event.
        /// </summary>
        public void Publish()
        {
            // Enforce Invariant
            if (IsPublished)
            {
                throw new InvalidOperationException(
                    "Cannot publish an event that is already published."
                );
            }

            IsPublished = true;

            // Optionally, raise a domain event for publishing
            // AddDomainEvent(new EventPublished(this.Id));
        }
    }
}
