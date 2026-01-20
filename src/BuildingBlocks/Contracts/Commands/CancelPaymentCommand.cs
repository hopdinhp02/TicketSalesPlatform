namespace TicketSalesPlatform.Contracts.Commands
{
    public record CancelPaymentCommand(Guid OrderId, string Reason);
}
