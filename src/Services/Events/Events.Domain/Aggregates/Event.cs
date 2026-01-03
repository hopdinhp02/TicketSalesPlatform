using SharedKernel;
using TicketSalesPlatform.Events.Domain.DomainEvents;
using NewId = MassTransit.NewId;

namespace TicketSalesPlatform.Events.Domain.Aggregates
{
    public sealed class Event : AggregateRoot<Guid>
    {
        public string Title { get; private set; } = default!;
        public string Description { get; private set; } = default!;
        public DateTime Date { get; private set; }
        public bool IsPublished { get; private set; }

        private readonly List<TicketType> _ticketTypes = new();
        public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

        private Event()
            : base(Guid.Empty) { }

        public static Event Create(string title, string description, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty.", nameof(title));

            if (date < DateTime.UtcNow)
                throw new InvalidOperationException("Cannot create an event in the past.");

            var aggregate = new Event();

            var @event = new EventCreated(NewId.NextGuid(), title, description, date);

            aggregate.AddDomainEvent(@event);
            aggregate.Apply(@event);

            return aggregate;
        }

        public void Publish()
        {
            if (!_ticketTypes.Any())
                throw new InvalidOperationException(
                    "Cannot publish an event with no ticket types."
                );

            if (IsPublished)
                throw new InvalidOperationException(
                    "Cannot publish an event that is already published."
                );

            var @event = new EventPublished(Id);

            AddDomainEvent(@event);
            Apply(@event);
        }

        public void AddTicketType(string name, decimal price, int quantity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Ticket type name is required.");

            if (price < 0)
                throw new ArgumentException("Price cannot be negative.");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            var @event = new TicketTypeAdded(Id, NewId.NextGuid(), name, price, quantity);

            AddDomainEvent(@event);
            Apply(@event);
        }

        private void Apply(EventCreated e)
        {
            Id = e.EventId;
            Title = e.Title;
            Description = e.Description;
            Date = e.Date;
            IsPublished = false;
        }

        private void Apply(TicketTypeAdded e)
        {
            _ticketTypes.Add(new TicketType(e.TicketTypeId, e.Name, e.Price, e.Quantity));
        }

        private void Apply(EventPublished e)
        {
            IsPublished = true;
        }
    }
}
