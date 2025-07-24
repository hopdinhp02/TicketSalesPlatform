using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using MassTransit;
using MediatR;
using Orders.Domain.Aggregates;
using TicketFlow.Orders.Application.Abstractions;
using TicketFlow.Orders.Application.GetOrderById;
using TicketFlow.Orders.Application.PlaceOrder;
using TicketFlow.Orders.Infrastructure.Persistence;
using TicketFlow.Orders.Infrastructure.Projections;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddAuthentication("Bearer")
    .AddJwtBearer(
        "Bearer",
        options =>
        {
            options.Authority = "https://localhost:5001"; // Identity.Api address
            options.Audience = "orders"; // API resource name

            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        }
    );

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Add MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(TicketFlow.Orders.Application.AssemblyReference.Assembly)
);

// 2. Add Marten
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
            cfg.Host(
                "localhost",
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

// 3. Register Repositories and Unit of Work
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
