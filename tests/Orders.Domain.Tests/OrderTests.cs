namespace TicketSalesPlatform.Orders.Domain.Tests
{
    public class OrderTests
    {
        private readonly Guid _customerId = Guid.NewGuid();

        //[Fact]
        //public void Create_Should_ThrowInvalidOperationException_WhenItemListIsNull()
        //{
        //    // Arrange
        //    List<OrderItem> nullItems = null;

        //    // Act
        //    Action act = () => Order.Create(_customerId, nullItems);

        //    // Assert
        //    act.Should()
        //        .Throw<InvalidOperationException>()
        //        .WithMessage("Cannot create an order with no items.");
        //}

        //[Fact]
        //public void Create_Should_ThrowInvalidOperationException_WhenItemListIsEmpty()
        //{
        //    // Arrange
        //    var emptyItems = new List<OrderItem>();

        //    // Act
        //    Action act = () => Order.Create(_customerId, emptyItems);

        //    // Assert
        //    act.Should()
        //        .Throw<InvalidOperationException>()
        //        .WithMessage("Cannot create an order with no items.");
        //}

        //[Fact]
        //public void Create_Should_SuccessfullyCreateOrder_WhenItemsAreProvided()
        //{
        //    // Arrange
        //    var items = new List<OrderItem>
        //    {
        //        new(Guid.Empty, Guid.NewGuid(), "The Grand Concert", 50, 2),
        //        new(Guid.Empty, Guid.NewGuid(), "The Grand Concert", 100, 1),
        //    };

        //    // Act
        //    var order = Order.Create(_customerId, items);

        //    // Assert
        //    order.Should().NotBeNull();
        //    order.CustomerId.Should().Be(_customerId);
        //    order.Status.Should().Be(OrderStatus.Placed);
        //    order.OrderItems.Should().HaveCount(2);
        //    order.TotalPrice.Should().Be(200); // (50 * 2) + (100 * 1)

        //    // Assert that the correct domain event was raised
        //    var domainEvent = order.GetDomainEvents().FirstOrDefault();
        //    domainEvent.Should().NotBeNull();
        //    domainEvent.Should().BeOfType<OrderPlaced>();

        //    var orderPlacedEvent = (OrderPlaced)domainEvent;
        //    orderPlacedEvent.OrderId.Should().Be(order.Id);
        //    orderPlacedEvent.TotalPrice.Should().Be(200);
        //}
    }
}
