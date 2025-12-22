using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Entities;

namespace TicketSalesPlatform.Inventory.Api.Endpoints
{
    public static class InventoryEndpoints
    {
        public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/inventory");

            group.MapPost("/reserve", ReserveSeat).RequireAuthorization();
            group.MapPost("/release", ReleaseSeat).RequireAuthorization();
            group.MapGet("/seats/{eventId}", GetSeats);
            group.MapPost("/init", InitData); // Helper to create dummy data
        }

        private static async Task<IResult> ReserveSeat(
            [FromBody] ReserveSeatRequest request,
            InventoryDbContext db,
            IPublisher publisher
        )
        {
            var seat = await db.Seats.FindAsync(request.SeatId);
            if (seat is null)
                return Results.NotFound(new { Error = "Seat not found" });

            try
            {
                seat.Reserve(request.UserId);
                await db.SaveChangesAsync();

                foreach (var domainEvent in seat.GetDomainEvents())
                {
                    await publisher.Publish(domainEvent);
                }
                seat.ClearDomainEvents();

                return Results.Ok(
                    new { Message = "Seat reserved.", ExpiresAt = seat.ReservationExpiresAt }
                );
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { Error = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(
                    new { Error = "Seat was modified by another transaction." }
                );
            }
        }

        private static async Task<IResult> ReleaseSeat(
            [FromBody] ReleaseSeatRequest request,
            InventoryDbContext db
        )
        {
            var seat = await db.Seats.FindAsync(request.SeatId);
            if (seat is null)
                return Results.NotFound();

            try
            {
                seat.Release(request.UserId);
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = "Seat released." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        }

        private static async Task<IResult> GetSeats(Guid eventId, InventoryDbContext db)
        {
            var seats = await db
                .Seats.Where(s => s.EventId == eventId)
                .Select(s => new
                {
                    s.Id,
                    s.SeatNo,
                    s.Status,
                })
                .ToListAsync();

            return Results.Ok(seats);
        }

        // Quick helper to seed data for testing
        private static async Task<IResult> InitData(InventoryDbContext db)
        {
            if (!db.Seats.Any())
            {
                var eventId = Guid.NewGuid();
                db.Seats.Add(new Seat("A1", eventId));
                db.Seats.Add(new Seat("A2", eventId));
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = "Seeded 2 seats", EventId = eventId });
            }
            return Results.Ok(new { Message = "Data already exists" });
        }
    }

    public record ReserveSeatRequest(Guid SeatId, Guid UserId);

    public record ReleaseSeatRequest(Guid SeatId, Guid UserId);
}
