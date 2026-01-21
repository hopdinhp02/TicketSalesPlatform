using MediatR;
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

        private static async Task<IResult> PlaceOrder(PlaceOrderCommand command, IMediator mediator)
        {
            var orderId = await mediator.Send(command);
            return Results.Created($"/api/orders/{orderId}", new { id = orderId });
        }

        private static async Task<IResult> GetOrderById(Guid id, IMediator mediator)
        {
            var result = await mediator.Send(new GetOrderByIdQuery(id));
            return result is not null ? Results.Ok(result) : Results.NotFound();
        }
    }
}
