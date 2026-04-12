using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Jobs;

namespace TicketSalesPlatform.Inventory.Api.Extensions
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            var connectionString = configuration.GetConnectionString("Database");

            services.AddDbContext<InventoryDbContext>(options =>
                options.UseNpgsql(connectionString)
            );

            services.AddMassTransit(x =>
            {
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("inventory", false));

                x.AddConsumers(typeof(Program).Assembly);

                x.AddEntityFrameworkOutbox<InventoryDbContext>(o =>
                {
                    o.UsePostgres();
                    o.UseBusOutbox();
                    o.IsolationLevel = System.Data.IsolationLevel.Serializable;
                });

                x.AddConfigureEndpointsCallback((context, name, cfg) =>
                {
                    cfg.UseEntityFrameworkOutbox<InventoryDbContext>(context);
                });

                x.UsingRabbitMq(
                    (context, cfg) =>
                    {
                        var host = configuration["MessageBroker:Host"];

                        cfg.Host(
                            host,
                            "/",
                            h =>
                            {
                                h.Username("guest");
                                h.Password("guest");
                            }
                        );

                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                        cfg.ConfigureEndpoints(context);
                    }
                );
            });

            services.AddHostedService<ExpiredReservationCleanupService>();

            return services;
        }
    }
}
