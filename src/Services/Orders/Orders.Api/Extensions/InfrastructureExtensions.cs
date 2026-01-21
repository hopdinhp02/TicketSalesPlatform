using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using MassTransit;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Application.Sagas;
using TicketSalesPlatform.Orders.Domain.Aggregates;
using TicketSalesPlatform.Orders.Infrastructure.Persistence;
using TicketSalesPlatform.Orders.Infrastructure.Projections;

namespace TicketSalesPlatform.Orders.Api.Extensions
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services
                .AddMarten(options =>
                {
                    var connectionString = configuration.GetConnectionString("Database");
                    options.Connection(connectionString!);

                    options.Projections.Add<OrderDetailsProjection>(ProjectionLifecycle.Async);

                    options.Schema.For<OrderState>().Identity(x => x.CorrelationId);
                })
                .UseLightweightSessions()
                .AddAsyncDaemon(DaemonMode.Solo);

            services.AddMassTransit(x =>
            {
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("order", false));

                x.AddSagaStateMachine<OrderStateMachine, OrderState>().MartenRepository();

                x.AddConsumers(TicketSalesPlatform.Orders.Application.AssemblyReference.Assembly);

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

                        cfg.UseInMemoryOutbox(context);

                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                        cfg.ConfigureEndpoints(context);
                    }
                );
            });

            services.AddScoped<IRepository<Order>, OrderRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
