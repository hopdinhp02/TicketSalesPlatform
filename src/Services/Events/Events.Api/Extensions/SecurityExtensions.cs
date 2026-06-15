using SharedKernel.Extensions;

namespace TicketSalesPlatform.Events.Api.Extensions
{
    public static class SecurityExtensions
    {
        public static IServiceCollection AddSecurityServices(
            this IServiceCollection services,
            IConfiguration config,
            IWebHostEnvironment env
        )
        {
            services.AddCustomAuthenticationAndAuthorization(config, env, "events");
            return services;
        }
    }
}
