namespace TicketSalesPlatform.Events.Api.Extensions
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    TicketSalesPlatform.Events.Application.AssemblyReference.Assembly
                )
            );
            return services;
        }
    }
}
