using SharedKernel;

namespace TicketSalesPlatform.Events.Domain.Aggregates
{
    public sealed class TicketType : Entity<Guid>
    {
        public string Name { get; private set; }
        public decimal Price { get; private set; }
        public int Quantity { get; private set; }

        internal TicketType(Guid id, string name, decimal price, int quantity)
            : base(id)
        {
            Name = name;
            Price = price;
            Quantity = quantity;
        }
    }
}
