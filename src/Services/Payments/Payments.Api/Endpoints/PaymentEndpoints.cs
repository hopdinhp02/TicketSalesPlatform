using MediatR;
using Microsoft.AspNetCore.Mvc;
using TicketSalesPlatform.Payments.Api.Data;
using TicketSalesPlatform.Payments.Api.Entities;

namespace TicketSalesPlatform.Payments.Api.Endpoints
{
    public static class PaymentEndpoints
    {
        public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/payments");

            group.MapPost("/", ProcessPayment).RequireAuthorization();
            group.MapPost("/refund", RefundPayment).RequireAuthorization();
        }

        private static async Task<IResult> ProcessPayment(
            [FromBody] CreatePaymentRequest request,
            PaymentDbContext db,
            IPublisher publisher
        )
        {
            Payment payment;
            try
            {
                payment = new Payment(request.OrderId, request.UserId, request.Amount);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }

            // Mocking external payment gateway
            bool paymentGatewaySuccess = true;

            if (paymentGatewaySuccess)
            {
                payment.Complete();
            }
            else
            {
                payment.Fail("Payment gateway declined transaction.");
            }

            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            foreach (var domainEvent in payment.GetDomainEvents())
            {
                await publisher.Publish(domainEvent);
            }
            payment.ClearDomainEvents();

            return payment.Status == PaymentStatus.Completed
                ? Results.Ok(new { Message = "Payment successful", PaymentId = payment.Id })
                : Results.BadRequest(
                    new { Message = "Payment failed", Reason = payment.FailureReason }
                );
        }

        private static async Task<IResult> RefundPayment(
            [FromBody] RefundRequest request,
            PaymentDbContext db,
            IPublisher publisher
        )
        {
            var payment = await db.Payments.FindAsync(request.PaymentId);

            if (payment is null)
                return Results.NotFound();

            try
            {
                payment.Refund();

                await db.SaveChangesAsync();

                foreach (var domainEvent in payment.GetDomainEvents())
                {
                    await publisher.Publish(domainEvent);
                }
                payment.ClearDomainEvents();

                return Results.Ok(new { Message = "Refund processed successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        }
    }

    public record CreatePaymentRequest(Guid OrderId, Guid UserId, decimal Amount);

    public record RefundRequest(Guid PaymentId);
}
