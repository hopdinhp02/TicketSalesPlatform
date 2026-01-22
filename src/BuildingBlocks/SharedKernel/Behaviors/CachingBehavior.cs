using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedKernel.Caching;

namespace SharedKernel.Behaviors
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICachedQuery<TResponse>
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(
            IDistributedCache cache,
            ILogger<CachingBehavior<TRequest, TResponse>> _logger
        )
        {
            _cache = cache;
            this._logger = _logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken
        )
        {
            var cachedData = await _cache.GetStringAsync(request.CacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("Cache HIT: {Key}", request.CacheKey);
                return JsonSerializer.Deserialize<TResponse>(cachedData)!;
            }

            _logger.LogWarning("Cache MISS: {Key}", request.CacheKey);
            var response = await next();

            if (response != null)
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = request.Expiration ?? TimeSpan.FromMinutes(5),
                };

                var serializedData = JsonSerializer.Serialize(response);
                await _cache.SetStringAsync(
                    request.CacheKey,
                    serializedData,
                    options,
                    cancellationToken
                );
            }

            return response;
        }
    }
}
