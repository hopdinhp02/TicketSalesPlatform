using MediatR;

namespace SharedKernel
{
    /// <summary>
    /// Represents a domain event, which is something that has happened in the domain
    /// that other parts of the same domain might be interested in.
    /// </summary>
    public interface IDomainEvent : INotification
    {
        Guid Id { get; }

        DateTime OccurredOn { get; }
    }
}
