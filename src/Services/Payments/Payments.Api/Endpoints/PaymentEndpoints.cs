using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Payments.Api.Data;
using TicketSalesPlatform.Payments.Api.Entities;
using TicketSalesPlatform.Payments.Api.Infrastructure.Clients.Order;

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
            IPublisher publisher,
            IOrderClient orderClient
        )
        {
            var orderInfo = await orderClient.GetOrderAsync(request.OrderId);

            if (orderInfo is null)
            {
                return Results.BadRequest(new { Error = "Order not found or invalid." });
            }

            if (orderInfo.Status == "Cancelled")
            {
                return Results.BadRequest(new { Error = "Order is Cancelled. Payment denied." });
            }

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == request.OrderId);

            if (payment is null)
            {
                try
                {
                    payment = new Payment(
                        request.OrderId,
                        orderInfo.CustomerId,
                        orderInfo.TotalPrice
                    );
                    db.Payments.Add(payment);
                    await db.SaveChangesAsync();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Error = ex.Message });
                }
                catch (DbUpdateException)
                {
                    // RACE CONDITION
                    db.ChangeTracker.Clear();
                    payment = await db.Payments.FirstAsync(p => p.OrderId == request.OrderId);
                }
            }

            if (payment.Status == PaymentStatus.Completed)
            {
                return Results.BadRequest(new { Error = "Order is already paid." });
            }

            if (
                payment.Status == PaymentStatus.Cancelled
                || payment.Status == PaymentStatus.Refunded
            )
            {
                return Results.BadRequest(
                    new { Error = "Order has been cancelled or refunded. Payment denied." }
                );
            }

            // Mocking external payment gateway
            bool paymentGatewaySuccess = true;

            if (paymentGatewaySuccess)
            {
                try
                {
                    payment.Complete();
                }
                catch (InvalidOperationException ex)
                {
                    // Refund
                    // await _gateway.RefundAsync(payment.TransactionId);

                    // _logger.LogWarning("Refunded payment because Order logic rejected completion. Error: {Error}", ex.Message);

                    return Results.BadRequest(
                        new
                        {
                            Error = "Payment processed but Order was invalid. Refund initiated.",
                            Detail = ex.Message,
                        }
                    );
                }
            }
            else
            {
                try
                {
                    payment.Fail("Payment gateway declined transaction.");
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { Error = ex.Message });
                }
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await db.Entry(payment).ReloadAsync();

                if (
                    payment.Status == PaymentStatus.Cancelled
                    || payment.Status == PaymentStatus.Refunded
                )
                {
                    if (paymentGatewaySuccess)
                    {
                        // Refund
                        // await _gateway.RefundAsync(payment.TransactionId);

                        return Results.BadRequest(
                            new { Error = "Order cancelled concurrently. Refund initiated." }
                        );
                    }

                    return Results.BadRequest(
                        new { Error = "Payment failed. Order was cancelled." }
                    );
                }

                return Results.Conflict(
                    new { Error = "Data changed by another process. Please try again." }
                );
            }

            foreach (var domainEvent in payment.GetDomainEvents())
            {
                await publisher.Publish(domainEvent);
            }
            payment.ClearDomainEvents();

            if (payment.Status == PaymentStatus.Completed)
            {
                return Results.Ok(new { Message = "Payment successful", PaymentId = payment.Id });
            }

            return Results.BadRequest(
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

            if (payment.Status == PaymentStatus.Refunded)
                return Results.Ok(new { Message = "Already refunded." });

            try
            {
                // var gatewayResult = await gateway.RefundAsync(payment.TransactionId, request.Amount);
                // if (!gatewayResult.Success) throw new Exception(gatewayResult.Error);

                payment.Refund(request.reason);

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
            // catch (GatewayException ex)
        }
    }

    public record CreatePaymentRequest(Guid OrderId);

    public record RefundRequest(Guid PaymentId, string reason);
}
