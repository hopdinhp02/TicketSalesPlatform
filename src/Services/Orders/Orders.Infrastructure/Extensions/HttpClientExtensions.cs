using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using TicketSalesPlatform.Orders.Infrastructure.Authentication;

namespace TicketSalesPlatform.Orders.Infrastructure.Extensions
{
    public static class HttpClientExtensions
    {
        public static IHttpStandardResiliencePipelineBuilder AddDefaultResilience(
            this IHttpClientBuilder builder
        )
        {
            return builder
                .AddHttpMessageHandler<TokenPropagationHandler>()
                .AddStandardResilienceHandler(options =>
                {
                    options.Retry.MaxRetryAttempts = 5;
                    options.Retry.Delay = TimeSpan.FromSeconds(1);
                    options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;

                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                    options.CircuitBreaker.FailureRatio = 0.5;
                    options.CircuitBreaker.MinimumThroughput = 5;
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);

                    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                });
        }
    }
}
