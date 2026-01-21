using JasperFx.Events.Projections;
using Marten;
using MassTransit;
using TicketSalesPlatform.Events.Application.Abstractions;
using TicketSalesPlatform.Events.Infrastructure.Persistence;
using TicketSalesPlatform.Events.Infrastructure.Projections;

namespace TicketSalesPlatform.Events.Api.Extensions
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            services
                .AddMarten(options =>
                {
                    var connectionString = config.GetConnectionString("Database");
                    options.Connection(connectionString!);

                    options.Projections.Add<EventSummaryProjection>(ProjectionLifecycle.Async);
                    options.Projections.Add<EventDetailProjection>(ProjectionLifecycle.Async);
                })
                .UseLightweightSessions()
                .AddAsyncDaemon(JasperFx.Events.Daemon.DaemonMode.Solo);

            services.AddMassTransit(x =>
            {
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("event", false));

                x.AddConsumers(TicketSalesPlatform.Events.Application.AssemblyReference.Assembly);

                x.UsingRabbitMq(
                    (context, cfg) =>
                    {
                        cfg.Host(
                            config["MessageBroker:Host"],
                            "/",
                            h =>
                            {
                                h.Username("guest");
                                h.Password("guest");
                            }
                        );

                        cfg.UseInMemoryOutbox(context);
                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                        cfg.ConfigureEndpoints(context);
                    }
                );
            });

            services.AddScoped<
                IRepository<TicketSalesPlatform.Events.Domain.Aggregates.Event>,
                EventRepository
            >();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
