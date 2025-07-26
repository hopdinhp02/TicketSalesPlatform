using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using MassTransit;
using MediatR;
using Orders.Domain.Aggregates;
using Polly;
using TicketFlow.Orders.Application.Abstractions;
using TicketFlow.Orders.Application.GetOrderById;
using TicketFlow.Orders.Application.PlaceOrder;
using TicketFlow.Orders.Infrastructure.Authentication;
using TicketFlow.Orders.Infrastructure.Clients;
using TicketFlow.Orders.Infrastructure.Persistence;
using TicketFlow.Orders.Infrastructure.Projections;

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
    cfg.RegisterServicesFromAssembly(TicketFlow.Orders.Application.AssemblyReference.Assembly)
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

builder.Services.AddMassTransit(busConfigurator =>
{
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
    .AddHttpMessageHandler<TokenPropagationHandler>()
    .AddStandardResilienceHandler(options =>
    {
        // Configure the standard retry policy
        options.Retry.MaxRetryAttempts = 5;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.BackoffType = DelayBackoffType.Exponential;

        // Configure the standard circuit breaker policy
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
    });

// Register Repositories and Unit of Work
builder.Services.AddScoped<IRepository<Order>, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();

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
