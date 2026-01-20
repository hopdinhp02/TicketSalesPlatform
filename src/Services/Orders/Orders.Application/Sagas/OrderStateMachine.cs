using MassTransit;
using TicketSalesPlatform.Contracts.Commands;
using TicketSalesPlatform.Contracts.Events;

namespace TicketSalesPlatform.Orders.Application.Sagas
{
    public class OrderStateMachine : MassTransitStateMachine<OrderState>
    {
        public State Submitted { get; private set; }
        public State StockReserved { get; private set; }
        public State Failed { get; private set; }
        public State Completed { get; private set; }
        public State Paid { get; private set; }
        public State RefundPending { get; private set; }
        public State Refunded { get; private set; }

        public Event<OrderPlacedIntegrationEvent> OrderPlacedEvent { get; private set; }
        public Event<StockReservedIntegrationEvent> StockReservedEvent { get; private set; }
        public Event<OrderReservationFailedIntegrationEvent> ReservationFailedEvent
        {
            get;
            private set;
        }
        public Event<OrderPaymentSucceededIntegrationEvent> PaymentSucceededEvent
        {
            get;
            private set;
        }
        public Event<PaymentFailedIntegrationEvent> PaymentFailedEvent { get; private set; }

        public Event<OrderReservationExpiredIntegrationEvent> ReservationExpiredEvent
        {
            get;
            private set;
        }

        public Event<StockConfirmedIntegrationEvent> StockConfirmedEvent { get; private set; }
        public Event<RefundRequiredIntegrationEvent> RefundRequiredEvent { get; private set; }
        public Event<PaymentRefundedIntegrationEvent> PaymentRefundedEvent { get; private set; }

        public OrderStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => OrderPlacedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => StockReservedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => ReservationFailedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => PaymentSucceededEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => PaymentFailedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => ReservationExpiredEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => StockConfirmedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => RefundRequiredEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => PaymentRefundedEvent, x => x.CorrelateById(m => m.Message.OrderId));

            Initially(
                When(OrderPlacedEvent)
                    .Then(context =>
                    {
                        context.Saga.OrderId = context.Message.OrderId;
                        context.Saga.CustomerId = context.Message.CustomerId;
                        context.Saga.TotalPrice = context.Message.TotalPrice;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new ReserveStockCommand(
                        context.Message.OrderId,
                        context.Message.CustomerId,
                        context.Message.Items
                    ))
                    .TransitionTo(Submitted)
            );

            During(
                Submitted,
                When(StockReservedEvent)
                    .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                    .Publish(context => new ProcessPaymentCommand(
                        context.Saga.OrderId,
                        context.Saga.CustomerId,
                        context.Saga.TotalPrice
                    ))
                    .TransitionTo(StockReserved),
                When(ReservationFailedEvent)
                    .Then(context =>
                    {
                        context.Saga.ErrorReason = context.Message.Reason;
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                    })
                    .TransitionTo(Failed)
            );

            During(
                StockReserved,
                When(PaymentSucceededEvent)
                    .Then(context =>
                    {
                        context.Saga.PaymentId = context.Message.PaymentId;
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new ConfirmStockCommand(context.Saga.OrderId))
                    .TransitionTo(Paid),
                When(PaymentFailedEvent)
                    .Then(context =>
                    {
                        context.Saga.ErrorReason = context.Message.Reason;
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new ReleaseStockCommand(context.Saga.OrderId))
                    .TransitionTo(Failed),
                When(ReservationExpiredEvent)
                    .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                    .Publish(context => new CancelPaymentCommand(
                        context.Saga.OrderId,
                        "Reservation Expired"
                    ))
                    .TransitionTo(Failed)
            );

            During(
                Paid,
                When(StockConfirmedEvent)
                    .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                    .TransitionTo(Completed),
                When(RefundRequiredEvent)
                    .Then(context =>
                    {
                        context.Saga.ErrorReason = context.Message.Reason;
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new RefundPaymentCommand(
                        context.Saga.OrderId,
                        context.Saga.ErrorReason
                    ))
                    .TransitionTo(RefundPending)
            );

            During(
                RefundPending,
                When(PaymentRefundedEvent)
                    .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                    .TransitionTo(Refunded)
            );

            During(
                Completed,
                When(PaymentRefundedEvent)
                    .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                    .Publish(context => new ReleaseStockCommand(context.Saga.OrderId))
                    .TransitionTo(Refunded)
            );
        }
    }
}
