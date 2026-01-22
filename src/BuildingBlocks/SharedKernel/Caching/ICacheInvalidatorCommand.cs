using MediatR;

namespace SharedKernel.Caching
{
    public interface ICacheInvalidatorCommand<out TResponse> : IRequest<TResponse>
    {
        IEnumerable<string> CacheKeys { get; }
    }
}
