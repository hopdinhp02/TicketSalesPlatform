using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace SharedKernel.Extensions
{
    public static class ObservabilityExtensions
    {
        public static WebApplicationBuilder AddObservability(
            this WebApplicationBuilder builder,
            string serviceName
        )
        {
            var jaegerEndpoint = builder.Configuration.GetValue<string>(
                "OpenTelemetry:Endpoint",
                "http://localhost:4317"
            );

            var seqEndpoint = builder.Configuration.GetValue<string>(
                "Serilog:SeqServerUrl",
                "http://localhost:5341"
            );

            builder
                .Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(opts =>
                        {
                            opts.Endpoint = new Uri(jaegerEndpoint!);
                        });
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddPrometheusExporter();
                });

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .WriteTo.Console()
                .WriteTo.Seq(seqEndpoint!)
                .CreateLogger();

            builder.Host.UseSerilog();

            return builder;
        }

        public static WebApplication UseObservability(this WebApplication app)
        {
            app.UseSerilogRequestLogging();
            app.MapPrometheusScrapingEndpoint();

            return app;
        }
    }
}
