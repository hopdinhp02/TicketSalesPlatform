using System.Text.Json;
using Marten;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using TicketSalesPlatform.Events.Domain.ReadModels;
using AppTicketTypeDto = TicketSalesPlatform.Events.Application.GetTicketTypeById.TicketTypeDto;

namespace TicketSalesPlatform.Events.Application.GetTicketTypesBulk
{
    public class GetTicketTypesBulkQueryHandler
        : IRequestHandler<GetTicketTypesBulkQuery, IEnumerable<AppTicketTypeDto>>
    {
        private readonly IQuerySession _session;
        private readonly IDistributedCache _cache;

        public GetTicketTypesBulkQueryHandler(IQuerySession session, IDistributedCache cache)
        {
            _session = session;
            _cache = cache;
        }

        public async Task<IEnumerable<AppTicketTypeDto>> Handle(
            GetTicketTypesBulkQuery request,
            CancellationToken token
        )
        {
            var results = new List<AppTicketTypeDto>();
            var missingIds = new List<Guid>();

            foreach (var id in request.TicketTypeIds)
            {
                var cacheKey = $"ticket-type-{id}";
                var cachedBytes = await _cache.GetAsync(cacheKey, token);
                if (cachedBytes != null)
                {
                    var dto = JsonSerializer.Deserialize<AppTicketTypeDto>(cachedBytes);
                    if (dto != null)
                    {
                        results.Add(dto);
                        continue;
                    }
                }
                missingIds.Add(id);
            }

            if (missingIds.Count > 0)
            {
                var views = await _session
                    .Query<TicketTypeView>()
                    .Where(t => t.IsPublished && missingIds.Contains(t.Id))
                    .ToListAsync(token);

                var dbTicketTypes = views
                    .Select(x => new AppTicketTypeDto(x.Id, x.EventId, x.Name, x.Price, x.Quantity))
                    .ToList();

                foreach (var dto in dbTicketTypes)
                {
                    results.Add(dto);

                    var cacheKey = $"ticket-type-{dto.Id}";
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(dto);
                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    };
                    await _cache.SetAsync(cacheKey, bytes, options, token);
                }
            }

            return results;
        }
    }
}
