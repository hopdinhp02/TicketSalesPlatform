using System.Collections.Concurrent;

namespace TicketFlow.Notifications.Api.Idempotency
{
    /// <summary>
    /// A simple in-memory service to track processed message IDs to ensure idempotency.
    /// In a production system, this would be backed by a persistent, distributed store
    /// like Redis or a database table to handle multiple service instances.
    /// </summary>
    public class InMemoryProcessedMessageService : IProcessedMessageService
    {
        private readonly ConcurrentDictionary<Guid, bool> _processedMessages = new();

        public Task<bool> HasBeenProcessedAsync(Guid messageId)
        {
            return Task.FromResult(_processedMessages.ContainsKey(messageId));
        }

        public Task MarkAsProcessedAsync(Guid messageId)
        {
            _processedMessages.TryAdd(messageId, true);
            return Task.CompletedTask;
        }
    }
}
