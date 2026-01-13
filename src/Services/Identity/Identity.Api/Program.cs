using OpenTelemetry.Trace;
using Serilog;
using TicketSalesPlatform.Identity.Api;

try
{
    var builder = WebApplication.CreateBuilder(args);

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
