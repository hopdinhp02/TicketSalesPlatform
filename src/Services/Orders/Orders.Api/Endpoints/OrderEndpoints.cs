using System.Security.Claims;
using MediatR;
using SharedKernel.Extensions;
using TicketSalesPlatform.Orders.Application.GetOrderById;
using TicketSalesPlatform.Orders.Application.PlaceOrder;

namespace TicketSalesPlatform.Orders.Api.Endpoints
{
    public static class OrderEndpoints
    {
        public static void MapOrderEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();

            group.MapPost("/", PlaceOrder).WithName("PlaceOrder");

            group.MapGet("/{id:guid}", GetOrderById).WithName("GetOrderById");
        }

        private static async Task<IResult> PlaceOrder(
            ClaimsPrincipal user,
            PlaceOrderRequest request,
            IMediator mediator
        )
        {
            var customerId = user.GetUserId();
            if (customerId == null)
            {
                return Results.Unauthorized();
            }

            var command = new PlaceOrderCommand(customerId.Value, request.Items);
            var orderId = await mediator.Send(command);
            return Results.Created($"/api/orders/{orderId}", new { id = orderId });
        }

        private static async Task<IResult> GetOrderById(
            Guid id,
            ClaimsPrincipal user,
            IMediator mediator
        )
        {
            var result = await mediator.Send(new GetOrderByIdQuery(id));
            if (result is null)
            {
                return Results.NotFound();
            }

            var userId = user.GetUserId();
            if (!user.IsAdmin() && result.CustomerId != userId)
            {
                return Results.Forbid();
            }

            return Results.Ok(result);
        }
    }

    public sealed record PlaceOrderRequest(List<OrderItemDto> Items);
}
