using TicketSalesPlatform.Orders.Application.Clients;
using TicketSalesPlatform.Orders.Infrastructure.Authentication;
using TicketSalesPlatform.Orders.Infrastructure.Clients;
using TicketSalesPlatform.Orders.Infrastructure.Extensions;

namespace TicketSalesPlatform.Orders.Api.Extensions
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
                .AddHttpClient<IEventsClient, EventsClient>(client =>
                {
                    var eventsApiUrl = configuration["Services:EventsApiUrl"];
                    client.BaseAddress = new Uri(eventsApiUrl!);
                })
                .AddDefaultResilience();



            return services;
        }
    }
}
