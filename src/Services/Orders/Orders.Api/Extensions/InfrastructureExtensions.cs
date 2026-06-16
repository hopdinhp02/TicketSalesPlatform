using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using MassTransit;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Application.Sagas;
using TicketSalesPlatform.Orders.Domain.Aggregates;
using TicketSalesPlatform.Orders.Infrastructure.Persistence;
using TicketSalesPlatform.Orders.Infrastructure.Projections;
using TicketSalesPlatform.Orders.Api.Options;
using TicketSalesPlatform.Contracts.Commands;
using StackExchange.Redis;
using Order = TicketSalesPlatform.Orders.Domain.Aggregates.Order;

namespace TicketSalesPlatform.Orders.Api.Extensions
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            var messageBrokerSettings = new MessageBrokerSettings();
            configuration.GetSection(MessageBrokerSettings.SectionName).Bind(messageBrokerSettings);

            EndpointConvention.Map<ReserveStockCommand>(new Uri($"queue:{messageBrokerSettings.InventoryReserveStockQueue}"));
            EndpointConvention.Map<ConfirmStockCommand>(new Uri($"queue:{messageBrokerSettings.InventoryConfirmStockQueue}"));
            EndpointConvention.Map<ReleaseStockCommand>(new Uri($"queue:{messageBrokerSettings.InventoryReleaseStockQueue}"));
            EndpointConvention.Map<ProcessPaymentCommand>(new Uri($"queue:{messageBrokerSettings.PaymentProcessPaymentQueue}"));
            EndpointConvention.Map<CancelPaymentCommand>(new Uri($"queue:{messageBrokerSettings.PaymentCancelPaymentQueue}"));
            EndpointConvention.Map<RefundPaymentCommand>(new Uri($"queue:{messageBrokerSettings.PaymentRefundPaymentQueue}"));

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

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConn = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                return ConnectionMultiplexer.Connect(redisConn);
            });

            services.AddMassTransit(x =>
            {
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("order", false));

                x.AddSagaStateMachine<OrderStateMachine, OrderState>().MartenRepository();

                x.AddConsumers(TicketSalesPlatform.Orders.Application.AssemblyReference.Assembly);

                x.AddConfigureEndpointsCallback(
                    (context, name, cfg) =>
                    {
                        cfg.ConcurrentMessageLimit = 64;
                        cfg.PrefetchCount = 128;
                    }
                );

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
