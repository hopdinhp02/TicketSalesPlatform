using SharedKernel.Caching;
using TicketSalesPlatform.Events.Domain.ReadModels;

namespace TicketSalesPlatform.Events.Application.GetEventById
{
    public sealed record GetEventByIdQuery(Guid Id) : ICachedQuery<EventSummary?>
    {
        public string CacheKey => $"events-{Id}";
        public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
    }
}
