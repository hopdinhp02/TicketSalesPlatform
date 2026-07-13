using MediatR;
using TicketSalesPlatform.Events.Application.CreateEvent;
using TicketSalesPlatform.Events.Application.GetEventById;
using TicketSalesPlatform.Events.Application.GetTicketTypeById;
using TicketSalesPlatform.Events.Application.PublishEvent;

namespace TicketSalesPlatform.Events.Api.Endpoints
{
    public static class EventEndpoints
    {
        public static void MapEventEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/events").WithTags("Events");

            group
                .MapPost("/", CreateEvent)
                .RequireAuthorization("RequireOrganizerRole")
                .WithName("CreateEvent");

            group.MapGet("/{id:guid}", GetEventById).WithName("GetEventById");

            group
                .MapPost("/{id:guid}/publish", PublishEvent)
                .RequireAuthorization("RequireOrganizerRole")
                .WithName("PublishEvent");

            app.MapGet("/api/events/ticket-types/{ticketTypeId:guid}", GetTicketTypeById)
                .WithTags("TicketTypes")
                .WithName("GetTicketTypeById");

            app.MapPost("/api/events/ticket-types/batch", GetTicketTypesBulk)
                .WithTags("TicketTypes")
                .WithName("GetTicketTypesBulk");
        }

        private static async Task<IResult> CreateEvent(
            CreateEventCommand command,
            IMediator mediator
        )
        {
            var eventId = await mediator.Send(command);
            return Results.Created($"/api/events/{eventId}", new { id = eventId });
        }

        private static async Task<IResult> GetEventById(Guid id, IMediator mediator)
        {
            var query = new GetEventByIdQuery(id);
            var result = await mediator.Send(query);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        }

        private static async Task<IResult> GetTicketTypeById(Guid ticketTypeId, IMediator mediator)
        {
            var query = new GetTicketTypeByIdQuery(ticketTypeId);
            var result = await mediator.Send(query);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        }

        private static async Task<IResult> GetTicketTypesBulk(
            [Microsoft.AspNetCore.Mvc.FromBody] IEnumerable<Guid> ticketTypeIds,
            IMediator mediator
        )
        {
            var query =
                new TicketSalesPlatform.Events.Application.GetTicketTypesBulk.GetTicketTypesBulkQuery(
                    ticketTypeIds
                );
            var result = await mediator.Send(query);
            return Results.Ok(result);
        }

        private static async Task<IResult> PublishEvent(Guid id, ISender sender)
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
    }
}
