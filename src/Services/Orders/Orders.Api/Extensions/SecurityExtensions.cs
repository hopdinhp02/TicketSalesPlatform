using SharedKernel.Extensions;

namespace TicketSalesPlatform.Orders.Api.Extensions
{
    public static class SecurityExtensions
    {
        public static IServiceCollection AddSecurityServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment
        )
        {
            services.AddCustomAuthenticationAndAuthorization(configuration, environment, "orders");
            return services;
        }
    }
}
