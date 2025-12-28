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
            group.MapGet(
                "/ticket-types/{ticketTypeId:guid}/availability",
                GetInventoryAvailability
            );
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
                seat.Reserve(request.UserId, request.OrderId);

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
            if (await db.Seats.AnyAsync())
            {
                return Results.Ok(new { Message = "Data already exists. Skip seeding." });
            }

            var eventId = Guid.NewGuid();

            var vipTicketTypeId = Guid.NewGuid();
            var standardTicketTypeId = Guid.NewGuid();

            var seats = new List<Seat>
            {
                new("A1", eventId, vipTicketTypeId),
                new("A2", eventId, vipTicketTypeId),
                new("B1", eventId, standardTicketTypeId),
                new("B2", eventId, standardTicketTypeId),
                new("B3", eventId, standardTicketTypeId),
            };

            db.Seats.AddRange(seats);
            await db.SaveChangesAsync();

            return Results.Ok(
                new
                {
                    Message = "Seeded successfully!",
                    EventId = eventId,
                    VipTicketTypeId = vipTicketTypeId,
                    StandardTicketTypeId = standardTicketTypeId,
                    TotalSeats = seats.Count,
                }
            );
        }

        public static async Task<IResult> GetInventoryAvailability(
            Guid ticketTypeId,
            InventoryDbContext db
        )
        {
            var count = await db.Seats.CountAsync(s =>
                s.TicketTypeId == ticketTypeId && s.Status == SeatStatus.Available
            );

            return Results.Ok(new { TicketTypeId = ticketTypeId, AvailableQuantity = count });
        }
    }

    public record ReserveSeatRequest(Guid SeatId, Guid UserId, Guid OrderId);

    public record ReleaseSeatRequest(Guid SeatId, Guid UserId);
}
