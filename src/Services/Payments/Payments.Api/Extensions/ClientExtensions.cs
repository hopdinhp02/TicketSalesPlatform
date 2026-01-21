using TicketSalesPlatform.Payments.Api.Infrastructure.Authentication;
using TicketSalesPlatform.Payments.Api.Infrastructure.Clients.Order;
using TicketSalesPlatform.Payments.Api.Infrastructure.Extensions;

namespace TicketSalesPlatform.Payments.Api.Extensions
{
    public static class ClientExtensions
    {
        public static IServiceCollection AddExternalClients(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddHttpContextAccessor();
            services.AddTransient<TokenPropagationHandler>();

            services
                .AddHttpClient<IOrderClient, OrderClient>(client =>
                {
                    var orderApiUrl = configuration["Services:OrderApiUrl"];
                    client.BaseAddress = new Uri(orderApiUrl!);
                })
                .AddDefaultResilience();

            return services;
        }
    }
}
