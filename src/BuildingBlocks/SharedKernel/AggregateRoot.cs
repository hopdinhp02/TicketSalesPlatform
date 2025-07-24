namespace SharedKernel
{
    /// <summary>
    /// Represents an aggregate root, which is a specific type of entity that serves
    /// as the entry point for all commands that modify the aggregate's state.
    /// </summary>
    /// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
    public abstract class AggregateRoot<TId> : Entity<TId>
        where TId : notnull
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        protected AggregateRoot(TId id)
            : base(id) { }

        /// <summary>
        /// Gets the collection of domain events that have been raised by this aggregate.
        /// </summary>
        public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.ToList();

        /// <summary>
        /// Clears the collection of domain TicketFlow.Events. This is typically called after the events
        /// have been dispatched.
        /// </summary>
        public void ClearDomainEvents() => _domainEvents.Clear();

        /// <summary>
        /// Adds a domain event to the aggregate. This should be called by the aggregate's
        /// methods when a significant state change occurs.
        /// </summary>
        /// <param name="domainEvent">The domain event to add.</param>
        protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    }
}
