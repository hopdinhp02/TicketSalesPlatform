namespace TicketSalesPlatform.Contracts.Commands
{
    public record RefundPaymentCommand(Guid OrderId, string Reason);
}
