using System.ComponentModel.DataAnnotations;
using SharedKernel;
using TicketSalesPlatform.Inventory.Api.Entities.DomainEvents;

namespace TicketSalesPlatform.Inventory.Api.Entities
{
    public class Seat : AggregateRoot<Guid>
    {
        public string SeatNo { get; private set; }
        public Guid EventId { get; private set; }
        public Guid TicketTypeId { get; private set; }
        public SeatStatus Status { get; private set; }
        public Guid? UserId { get; private set; }
        public Guid? OrderId { get; private set; }
        public DateTime? ReservationExpiresAt { get; private set; }

        [Timestamp]
        public uint Version { get; private set; }

        private Seat()
            : base(Guid.NewGuid()) { }

        public Seat(string seatNo, Guid eventId, Guid ticketTypeId)
            : base(Guid.NewGuid())
        {
            SeatNo = seatNo;
            EventId = eventId;
            TicketTypeId = ticketTypeId;
            Status = SeatStatus.Available;
        }

        public void Reserve(Guid userId, Guid orderId)
        {
            if (Status != SeatStatus.Available)
            {
                throw new InvalidOperationException("Seat is not available.");
            }

            Status = SeatStatus.Reserved;
            UserId = userId;
            OrderId = orderId;
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(15);

            AddDomainEvent(new SeatReserved(Id, userId, orderId, ReservationExpiresAt.Value));
        }

        public void Release(Guid userId)
        {
            if (Status != SeatStatus.Reserved || UserId != userId)
            {
                throw new InvalidOperationException("Invalid release attempt.");
            }

            Status = SeatStatus.Available;
            UserId = null;
            // Keep OrderId for record-keeping
            //OrderId = null;
            ReservationExpiresAt = null;

            // Optionally: AddDomainEvent(new SeatReleased(Id));
        }

        public void MarkAsSold()
        {
            if (Status != SeatStatus.Reserved)
                throw new InvalidOperationException("Seat must be Reserved before Sold");

            Status = SeatStatus.Sold;
            ReservationExpiresAt = null;

            // Optionally: AddDomainEvent(new SeatSold(Id));
        }

        public void Expire()
        {
            if (Status != SeatStatus.Reserved)
                return;

            Status = SeatStatus.Available;
            UserId = null;
            OrderId = null;
            ReservationExpiresAt = null;

            // AddDomainEvent(new SeatExpired(...));
        }

        public void Refund()
        {
            if (Status != SeatStatus.Sold && Status != SeatStatus.Reserved)
            {
                if (Status == SeatStatus.Available)
                    return;

                throw new InvalidOperationException(
                    $"Cannot refund seat {Id} because status is {Status}"
                );
            }

            Status = SeatStatus.Available;
            ReservationExpiresAt = null;
            UserId = null;
            // Audit Log
            // OrderId = null;

            // AddDomainEvent(new SeatRefunded(Id));
        }
    }

    public enum SeatStatus
    {
        Available,
        Reserved,
        Sold,
    }
}
