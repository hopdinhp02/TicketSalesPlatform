using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
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

            // This configures a queue for our consumer and binds it to the
            // correct exchange for the OrderPlacedIntegrationEvent.
            cfg.ConfigureEndpoints(context);
        }
    );
});

// --- END: MASSTRANSIT CONFIGURATION ---

builder
    .Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(serviceName: builder.Environment.ApplicationName)
    )
    .WithTracing(tracing =>
        tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddConsoleExporter()
    );

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapGet("/", () => "Notifications Service is running.");

app.Run();
