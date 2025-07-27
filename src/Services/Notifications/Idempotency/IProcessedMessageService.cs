namespace TicketFlow.Notifications.Api.Idempotency
{
    public interface IProcessedMessageService
    {
        Task<bool> HasBeenProcessedAsync(Guid messageId);
        Task MarkAsProcessedAsync(Guid messageId);
    }
}
