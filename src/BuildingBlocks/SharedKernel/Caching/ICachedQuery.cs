using MediatR;

namespace SharedKernel.Caching
{
    public interface ICachedQuery<out TResponse> : IRequest<TResponse>
    {
        string CacheKey { get; }
        TimeSpan? Expiration { get; }
    }
}
