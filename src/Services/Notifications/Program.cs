using MassTransit;
using SharedKernel.Extensions;
using TicketSalesPlatform.Notifications.Api.Consumers;
using TicketSalesPlatform.Notifications.Api.Idempotency;

var builder = WebApplication.CreateBuilder(args);

// --- START: MASSTRANSIT CONFIGURATION ---
builder.Services.AddMassTransit(busConfigurator =>
{
    // Register our consumer class.
    busConfigurator.AddConsumer<OrderPlacedConsumer>();

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
                    // If the consumer throws an exception, retry 3 times with a 5-second delay between retries.
                    // After 3 failures, the message will be moved to an automatically created _error queue.
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                    e.ConfigureConsumer<OrderPlacedConsumer>(context);
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
