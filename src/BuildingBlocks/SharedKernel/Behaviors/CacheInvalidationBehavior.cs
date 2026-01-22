using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedKernel.Caching;

namespace SharedKernel.Behaviors
{
    public class CacheInvalidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICacheInvalidatorCommand<TResponse>
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

        public CacheInvalidationBehavior(
            IDistributedCache cache,
            ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger
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
            var response = await next();

            if (request.CacheKeys != null && request.CacheKeys.Any())
            {
                try
                {
                    var tasks = request.CacheKeys.Select(key =>
                        _cache.RemoveAsync(key, cancellationToken)
                    );

                    await Task.WhenAll(tasks);

                    foreach (var key in request.CacheKeys)
                    {
                        _logger.LogInformation("Cache INVALIDATED: {Key}", key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to invalidate cache keys");
                }
            }

            return response;
        }
    }
}
