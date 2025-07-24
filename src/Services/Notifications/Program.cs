using MassTransit;
using TicketFlow.Notifications.Api.Consumers;

var builder = WebApplication.CreateBuilder(args);

// --- START: MASSTRANSIT CONFIGURATION ---
builder.Services.AddMassTransit(busConfigurator =>
{
    // Register our consumer class.
    busConfigurator.AddConsumer<OrderPlacedConsumer>();

    busConfigurator.UsingRabbitMq(
        (context, cfg) =>
        {
            cfg.Host(
                "localhost",
                "/",
                h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                }
            );

            // This configures a queue for our consumer and binds it to the
            // correct exchange for the OrderPlacedIntegrationEvent.
            cfg.ConfigureEndpoints(context);
        }
    );
});

// --- END: MASSTRANSIT CONFIGURATION ---

var app = builder.Build();

app.MapGet("/", () => "Notifications Service is running.");

app.Run();
