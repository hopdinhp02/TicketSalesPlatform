using JasperFx.Events.Projections;
using Marten;
using MassTransit;
using MediatR;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TicketSalesPlatform.Events.Application.Abstractions;
using TicketSalesPlatform.Events.Application.CreateEvent;
using TicketSalesPlatform.Events.Application.GetEventById;
using TicketSalesPlatform.Events.Application.GetTicketTypeById;
using TicketSalesPlatform.Events.Application.PublishEvent;
using TicketSalesPlatform.Events.Infrastructure.Persistence;
using TicketSalesPlatform.Events.Infrastructure.Projections;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder
    .Services.AddAuthentication("Bearer")
    .AddJwtBearer(
        "Bearer",
        options =>
        {
            // The address of your Identity Service.
            options.Authority = builder.Configuration["Authentication:Authority"];

            options.Audience = "events";

            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        }
    );

builder.Services.AddAuthorization();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        TicketSalesPlatform.Events.Application.AssemblyReference.Assembly
    )
);

builder
    .Services.AddMarten(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("Database");
        options.Connection(connectionString!);
        options.Projections.Add<EventSummaryProjection>(ProjectionLifecycle.Async);
        options.Projections.Add<EventDetailProjection>(ProjectionLifecycle.Async);
    })
    .UseLightweightSessions()
    .AddAsyncDaemon(JasperFx.Events.Daemon.DaemonMode.Solo); // Starts the background projection processor

builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("event", false));

    x.AddConsumers(TicketSalesPlatform.Events.Application.AssemblyReference.Assembly);
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

builder.Services.AddScoped<
    IRepository<TicketSalesPlatform.Events.Domain.Aggregates.Event>,
    EventRepository
>();
builder.Services.AddScoped<
    TicketSalesPlatform.Events.Application.Abstractions.IUnitOfWork,
    UnitOfWork
>();

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

// --- API ENDPOINT ---
app.MapPost(
        "/api/events",
        async (CreateEventCommand command, IMediator mediator) =>
        {
            var eventId = await mediator.Send(command);
            return Results.Created($"/api/events/{eventId}", new { id = eventId });
        }
    )
    .WithName("CreateEvent")
    .WithTags("Events")
    .RequireAuthorization();

app.MapGet(
        "/api/events/{id:guid}",
        async (Guid id, IMediator mediator) =>
        {
            var query = new GetEventByIdQuery(id);
            var result = await mediator.Send(query);

            return result is not null ? Results.Ok(result) : Results.NotFound();
        }
    )
    .WithName("GetEventById")
    .WithTags("Events")
    .RequireAuthorization();

app.MapGet(
        "/api/events/ticket-types/{ticketTypeId:guid}",
        async (Guid ticketTypeId, IMediator mediator) =>
        {
            var query = new GetTicketTypeByIdQuery(ticketTypeId);
            var result = await mediator.Send(query);

            return result is not null ? Results.Ok(result) : Results.NotFound();
        }
    )
    .WithName("GetTicketTypeById")
    .WithTags("TicketTypes")
    .RequireAuthorization();

app.MapPost(
        "/api/events/{id:guid}/publish",
        async (Guid id, ISender sender) =>
        {
            try
            {
                await sender.Send(new PublishEventCommand(id));
                return Results.Ok(new { Message = "Event published successfully." });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        }
    )
    .WithName("PublishEvent")
    .WithTags("Events")
    .RequireAuthorization();

// --- API ENDPOINT ---

app.Run();
