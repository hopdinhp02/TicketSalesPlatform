namespace TicketSalesPlatform.Orders.Api.Extensions
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    TicketSalesPlatform.Orders.Application.AssemblyReference.Assembly
                )
            );

            return services;
        }
    }
}
