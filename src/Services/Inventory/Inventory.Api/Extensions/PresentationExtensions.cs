namespace TicketSalesPlatform.Inventory.Api.Extensions
{
    public static class PresentationExtensions
    {
        public static IServiceCollection AddPresentationServices(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            return services;
        }
    }
}
