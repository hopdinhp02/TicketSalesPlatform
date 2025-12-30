using System.ComponentModel.DataAnnotations;
using SharedKernel;
using TicketSalesPlatform.Payments.Api.Entities.DomainEvents;

namespace TicketSalesPlatform.Payments.Api.Entities
{
    public class Payment : AggregateRoot<Guid>
    {
        public Guid OrderId { get; private set; }
        public Guid UserId { get; private set; }
        public decimal Amount { get; private set; }
        public PaymentStatus Status { get; private set; }
        public string? FailureReason { get; private set; }
        public DateTime CreatedAt { get; private set; }

        [Timestamp]
        public uint Version { get; private set; }

        private Payment()
            : base(Guid.NewGuid()) { }

        public Payment(Guid orderId, Guid userId, decimal amount)
            : base(Guid.NewGuid())
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero.");
            }

            OrderId = orderId;
            UserId = userId;
            Amount = amount;
            Status = PaymentStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void Complete()
        {
            if (Status == PaymentStatus.Completed)
                return;

            if (Status != PaymentStatus.Pending)
            {
                throw new InvalidOperationException(
                    $"Cannot complete payment. Current status: {Status}"
                );
            }

            Status = PaymentStatus.Completed;

            AddDomainEvent(new PaymentCompleted(Id, OrderId, Amount));
        }

        public void Fail(string reason)
        {
            if (Status == PaymentStatus.Failed)
                return;

            if (
                Status == PaymentStatus.Completed
                || Status == PaymentStatus.Refunded
                || Status == PaymentStatus.Cancelled
            )
            {
                throw new InvalidOperationException(
                    $"Cannot fail a payment that is already {Status}."
                );
            }

            Status = PaymentStatus.Failed;
            FailureReason = reason;

            AddDomainEvent(new PaymentFailed(Id, OrderId, reason));
        }

        public void Refund(string reason)
        {
            if (Status != PaymentStatus.Completed)
            {
                if (Status == PaymentStatus.Pending)
                {
                    Cancel($"Cancelled instead of Refunded. Reason: {reason}");
                    return;
                }

                if (Status == PaymentStatus.Refunded)
                    return;

                throw new InvalidOperationException(
                    $"Cannot refund a payment with status {Status}"
                );
            }

            Status = PaymentStatus.Refunded;
            FailureReason = reason;

            AddDomainEvent(new PaymentRefunded(Id, OrderId));
        }

        public void Cancel(string reason)
        {
            if (Status == PaymentStatus.Completed)
            {
                throw new InvalidOperationException("Cannot cancel a completed payment.");
            }

            if (Status != PaymentStatus.Cancelled)
            {
                Status = PaymentStatus.Cancelled;
                FailureReason = reason;

                // AddDomainEvent(new PaymentCancelled(Id));
            }
        }
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded,
        Cancelled,
    }
}
