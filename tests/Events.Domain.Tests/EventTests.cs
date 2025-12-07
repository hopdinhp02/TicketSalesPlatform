using FluentAssertions;
using TicketSalesPlatform.Events.Domain.Aggregates;
using TicketSalesPlatform.Events.Domain.DomainEvents;

namespace TicketSalesPlatform.Events.Domain.Tests;

public class EventTests
{
    [Fact]
    public void Create_Should_ThrowArgumentException_WhenTitleIsEmpty()
    {
        // Arrange
        string emptyTitle = "";
        string description = "A valid description.";
        DateTime futureDate = DateTime.UtcNow.AddDays(10);

        // Act
        Action act = () => Event.Create(emptyTitle, description, futureDate);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Title cannot be empty. (Parameter 'title')");
    }

    [Fact]
    public void Create_Should_ThrowInvalidOperationException_WhenDateIsInThePast()
    {
        // Arrange
        string title = "Past Event";
        string description = "This event should not be created.";
        DateTime pastDate = DateTime.UtcNow.AddDays(-1);

        // Act
        // We define the action that we expect to throw an exception.
        Action act = () => Event.Create(title, description, pastDate);

        // Assert
        // We assert that the action throws the specific exception we're expecting.
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Cannot create an event in the past.");
    }

    [Fact]
    public void Create_Should_RaiseEventCreatedDomainEvent_WhenEventIsSuccessfullyCreated()
    {
        // Arrange
        string title = "Future Event";
        string description = "A valid event description.";
        DateTime futureDate = DateTime.UtcNow.AddDays(10);

        // Act
        var newEvent = Event.Create(title, description, futureDate);

        // Assert
        // 1. Check that the event was created with the correct properties.
        newEvent.Should().NotBeNull();
        newEvent.Title.Should().Be(title);
        newEvent.IsPublished.Should().BeFalse();

        // 2. Check that the correct domain event was raised.
        var domainEvents = newEvent.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        var eventCreated = domainEvents.First() as EventCreated;
        eventCreated.Should().NotBeNull();
        eventCreated.EventId.Should().Be(newEvent.Id);
        eventCreated.Title.Should().Be(title);
    }

    [Fact]
    public void Publish_Should_SetIsPublishedToTrue_WhenEventIsNotAlreadyPublished()
    {
        // Arrange
        var newEvent = Event.Create("Test Event", "Desc", DateTime.UtcNow.AddDays(5));

        // Act
        newEvent.Publish();

        // Assert
        newEvent.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Publish_Should_ThrowInvalidOperationException_WhenEventIsAlreadyPublished()
    {
        // Arrange
        var newEvent = Event.Create("Test Event", "Desc", DateTime.UtcNow.AddDays(5));
        newEvent.Publish(); // Publish it once

        // Act
        Action act = () => newEvent.Publish(); // Try to publish it again

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Cannot publish an event that is already published.");
    }
}
