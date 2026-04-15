namespace TicketSalesPlatform.Orders.Api.Options
{
    public class MessageBrokerSettings
    {
        public const string SectionName = "MessageBroker";

        public string Host { get; init; } = string.Empty;
        public string InventoryReserveStockQueue { get; init; } = string.Empty;
        public string InventoryConfirmStockQueue { get; init; } = string.Empty;
        public string InventoryReleaseStockQueue { get; init; } = string.Empty;
        public string PaymentProcessPaymentQueue { get; init; } = string.Empty;
        public string PaymentCancelPaymentQueue { get; init; } = string.Empty;
        public string PaymentRefundPaymentQueue { get; init; } = string.Empty;
    }
}
