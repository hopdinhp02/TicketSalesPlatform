namespace TicketSalesPlatform.IntegrationEvents
{
    public sealed record OrderPlacedIntegrationEvent(
        Guid OrderId,
        Guid CustomerId,
        decimal TotalPrice,
        DateTime OrderDate,
        List<OrderTicketItemDto> Items
    );

    public sealed record OrderTicketItemDto(Guid TicketTypeId, int Quantity);
}
