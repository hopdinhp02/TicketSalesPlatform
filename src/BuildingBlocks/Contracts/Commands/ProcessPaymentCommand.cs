namespace TicketSalesPlatform.Contracts.Commands
{
    public record ProcessPaymentCommand(Guid OrderId, Guid CustomerId, decimal TotalPrice);
}
