using Marten;
using MediatR;
using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Application.CreateEvent;
using TicketFlow.Events.Application.GetEventById;
using TicketFlow.Events.Domain.Aggregates;
using TicketFlow.Events.Infrastructure.Persistence;
using TicketFlow.Events.Infrastructure.Projections;

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
    cfg.RegisterServicesFromAssembly(TicketFlow.Events.Application.AssemblyReference.Assembly)
);

builder
    .Services.AddMarten(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("Database");
        options.Connection(connectionString!);
        options.Projections.Add<EventSummaryProjection>(
            JasperFx.Events.Projections.ProjectionLifecycle.Async
        );
    })
    .UseLightweightSessions()
    .AddAsyncDaemon(JasperFx.Events.Daemon.DaemonMode.Solo); // Starts the background projection processor

builder.Services.AddScoped<IRepository<Event>, EventRepository>();
builder.Services.AddScoped<TicketFlow.Events.Application.Abstractions.IUnitOfWork, UnitOfWork>();

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
;

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
;

app.MapGet(
        "/api/events/{eventId:guid}/tickets/{ticketTypeId:guid}/availability",
        async (Guid eventId, Guid ticketTypeId, IQuerySession querySession) =>
        {
            var availableTickets = 10; // Simulated availability

            return Results.Ok(
                new { TicketTypeId = ticketTypeId, AvailableQuantity = availableTickets }
            );
        }
    )
    .WithName("GetTicketAvailability")
    .WithTags("Events")
    .RequireAuthorization();

// --- API ENDPOINT ---

app.Run();
