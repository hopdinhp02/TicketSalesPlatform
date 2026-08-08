using MassTransit;
using SharedKernel.Extensions;
using TicketSalesPlatform.Notifications.Api.Consumers;
using TicketSalesPlatform.Notifications.Api.Idempotency;

var builder = WebApplication.CreateBuilder(args);

// --- START: MASSTRANSIT CONFIGURATION ---
builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.AddConsumer<OrderPlacedConsumer>();
    busConfigurator.AddConsumer<OrderCompletedConsumer>();
    busConfigurator.AddConsumer<OrderFailedConsumer>();

    busConfigurator.UsingRabbitMq(
        (context, cfg) =>
        {
            var host = builder.Configuration["MessageBroker:Host"];
            cfg.Host(
                host,
                "/",
                h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                }
            );

            cfg.ReceiveEndpoint(
                "order-placed-notifications",
                e =>
                {
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    e.ConfigureConsumer<OrderPlacedConsumer>(context);
                }
            );

            cfg.ReceiveEndpoint(
                "order-completed-notifications",
                e =>
                {
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    e.ConfigureConsumer<OrderCompletedConsumer>(context);
                }
            );

            cfg.ReceiveEndpoint(
                "order-failed-notifications",
                e =>
                {
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    e.ConfigureConsumer<OrderFailedConsumer>(context);
                }
            );
        }
    );
});

// --- END: MASSTRANSIT CONFIGURATION ---

builder.Services.AddSingleton<IProcessedMessageService, InMemoryProcessedMessageService>();

builder.AddObservability(builder.Environment.ApplicationName);

var app = builder.Build();

app.UseObservability();

app.MapGet("/", () => "Notifications Service is running.");

app.Run();
