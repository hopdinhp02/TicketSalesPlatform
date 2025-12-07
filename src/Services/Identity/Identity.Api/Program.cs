using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TicketSalesPlatform.Identity.Api;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder
        .Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
            resource.AddService(serviceName: builder.Environment.ApplicationName)
        )
        .WithTracing(tracing =>
            tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter()
        );

    builder.Host.UseSerilog(
        (ctx, lc) =>
            lc
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}"
                )
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(ctx.Configuration)
    );

    var app = builder.ConfigureServices().ConfigurePipeline();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}
