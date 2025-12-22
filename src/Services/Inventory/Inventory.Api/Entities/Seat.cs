using System.ComponentModel.DataAnnotations;
using SharedKernel;
using TicketSalesPlatform.Inventory.Api.Entities.DomainEvents;

namespace TicketSalesPlatform.Inventory.Api.Entities
{
    public class Seat : AggregateRoot<Guid>
    {
        public string SeatNo { get; private set; }
        public Guid EventId { get; private set; }
        public SeatStatus Status { get; private set; }
        public Guid? UserId { get; private set; }
        public DateTime? ReservationExpiresAt { get; private set; }

        [Timestamp]
        public uint Version { get; private set; }

        private Seat()
            : base(Guid.NewGuid()) { }

        public Seat(string seatNo, Guid eventId)
            : base(Guid.NewGuid())
        {
            SeatNo = seatNo;
            EventId = eventId;
            Status = SeatStatus.Available;
        }

        public void Reserve(Guid userId)
        {
            if (Status != SeatStatus.Available)
            {
                throw new InvalidOperationException("Seat is not available.");
            }

            Status = SeatStatus.Reserved;
            UserId = userId;
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(15);

            AddDomainEvent(new SeatReserved(Id, userId, ReservationExpiresAt.Value));
        }

        public void Release(Guid userId)
        {
            if (Status != SeatStatus.Reserved || UserId != userId)
            {
                throw new InvalidOperationException("Invalid release attempt.");
            }

            Status = SeatStatus.Available;
            UserId = null;
            ReservationExpiresAt = null;

            // Optionally raise SeatReleasedDomainEvent here
        }
    }

    public enum SeatStatus
    {
        Available,
        Reserved,
        Sold,
    }
}
