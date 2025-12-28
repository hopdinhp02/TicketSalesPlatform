using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using MassTransit;
using MediatR;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TicketSalesPlatform.Orders.Application.Abstractions;
using TicketSalesPlatform.Orders.Application.Clients;
using TicketSalesPlatform.Orders.Application.GetOrderById;
using TicketSalesPlatform.Orders.Application.PlaceOrder;
using TicketSalesPlatform.Orders.Domain.Aggregates;
using TicketSalesPlatform.Orders.Infrastructure.Authentication;
using TicketSalesPlatform.Orders.Infrastructure.Clients;
using TicketSalesPlatform.Orders.Infrastructure.Extensions;
using TicketSalesPlatform.Orders.Infrastructure.Persistence;
using TicketSalesPlatform.Orders.Infrastructure.Projections;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddAuthentication("Bearer")
    .AddJwtBearer(
        "Bearer",
        options =>
        {
            options.Authority = builder.Configuration["Authentication:Authority"];
            options.Audience = "orders"; // API resource name

            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        }
    );

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        TicketSalesPlatform.Orders.Application.AssemblyReference.Assembly
    )
);

builder
    .Services.AddMarten(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("Database");
        options.Connection(connectionString!);
        options.Projections.Add<OrderDetailsProjection>(ProjectionLifecycle.Async);
    })
    .UseLightweightSessions()
    .AddAsyncDaemon(DaemonMode.Solo);

builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("order", false));

    x.AddConsumers(TicketSalesPlatform.Orders.Application.AssemblyReference.Assembly);
    x.UsingRabbitMq(
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

            cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

            cfg.ConfigureEndpoints(context);
        }
    );
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<TokenPropagationHandler>();

builder
    .Services.AddHttpClient<IEventsClient, EventsClient>(client =>
    {
        var eventsApiUrl = builder.Configuration["Services:EventsApiUrl"];
        client.BaseAddress = new Uri(eventsApiUrl!);
    })
    .AddDefaultResilience();

builder
    .Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:InventoryApiUrl"]!);
    })
    .AddDefaultResilience();

// Register Repositories and Unit of Work
builder.Services.AddScoped<IRepository<Order>, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// API Endpoints
app.MapPost(
        "/api/orders",
        async (PlaceOrderCommand command, IMediator mediator) =>
        {
            var orderId = await mediator.Send(command);
            return Results.Created($"/api/orders/{orderId}", new { id = orderId });
        }
    )
    .WithName("PlaceOrder")
    .WithTags("Orders")
    .RequireAuthorization();
;

app.MapGet(
        "/api/orders/{id:guid}",
        async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetOrderByIdQuery(id));
            return result is not null ? Results.Ok(result) : Results.NotFound();
        }
    )
    .WithName("GetOrderById")
    .WithTags("Orders")
    .RequireAuthorization();
;
app.Run();
