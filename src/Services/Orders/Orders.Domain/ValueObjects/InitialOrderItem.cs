using SharedKernel;

namespace TicketSalesPlatform.Orders.Domain.ValueObjects
{
    public class InitialOrderItem : ValueObject
    {
        public Guid TicketTypeId { get; }
        public string Name { get; }
        public decimal UnitPrice { get; }
        public int Quantity { get; }

        public InitialOrderItem(Guid ticketTypeId, string name, decimal unitPrice, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));

            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Ticket type's name is required.", nameof(name));

            TicketTypeId = ticketTypeId;
            Name = name;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return TicketTypeId;
            yield return Name;
            yield return UnitPrice;
            yield return Quantity;
        }
    }
}
