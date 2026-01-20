using MassTransit;

namespace TicketSalesPlatform.Orders.Application.Sagas
{
    public class OrderState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? PaymentId { get; set; }
        public decimal TotalPrice { get; set; }
        public string ErrorReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
