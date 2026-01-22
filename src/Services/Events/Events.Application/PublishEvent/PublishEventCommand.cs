using MediatR;
using SharedKernel.Caching;

namespace TicketSalesPlatform.Events.Application.PublishEvent
{
    public record PublishEventCommand(Guid EventId) : ICacheInvalidatorCommand<Unit>
    {
        public IEnumerable<string> CacheKeys => [$"events-{EventId}"];
    }
}
