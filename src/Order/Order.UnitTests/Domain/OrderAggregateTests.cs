using Order.Domain.Aggregates.Order.Domain.Aggregates;
using Order.Domain.Aggregates.Order.Domain.Enums;
using Order.Domain.Aggregates.Order.Domain.Events;
using Order.Domain.Aggregates.Order.Domain.ValueObjects;
using Xunit;

namespace Order.Domain.Aggregates.Order.UnitTests.Domain;

/// <summary>
/// Unit tests for the Order.Domain.Aggregates.Order aggregate root.
/// Best candidate: core business rules, state transitions, validation, and domain events.
/// </summary>
public class OrderAggregateTests
{
    private static List<OrderItem> CreateValidItems() =>
        [new OrderItem("prod-1", 2, 10m), new OrderItem("prod-2", 1, 5m)];

    [Fact]
    public void Create_ValidInputs_CreatesOrderWithPendingStatus()
    {
        var items = CreateValidItems();
        var order = Order.Domain.Aggregates.Order.Create("customer-123", items);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal("customer-123", order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(25m, order.TotalAmount); // 2*10 + 1*5
        Assert.Equal(2, order.OrderItems.Count);
        Assert.Single(order.DomainEvents.OfType<OrderCreatedDomainEvent>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrEmptyCustomerId_ThrowsArgumentException(string? customerId)
    {
        var items = CreateValidItems();
        Assert.Throws<ArgumentException>(() => Order.Domain.Aggregates.Order.Create(customerId!, items));
    }

    [Fact]
    public void Create_NullItems_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Order.Domain.Aggregates.Order.Create("customer-1", null!));
    }

    [Fact]
    public void Create_EmptyItems_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Order.Domain.Aggregates.Order.Create("customer-1", []));
    }

    [Fact]
    public void MarkAsPaid_WhenPending_SetsStatusToPaidAndRaisesEvent()
    {
        var order = Order.Domain.Aggregates.Order.Create("customer-1", CreateValidItems());
        order.MarkAsPaid();

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Single(order.DomainEvents.OfType<OrderPaidDomainEvent>());
    }

    [Fact]
    public void MarkAsPaid_WhenNotPending_ThrowsInvalidOperationException()
    {
        var order = Order.Domain.Aggregates.Order.Create("customer-1", CreateValidItems());
        order.MarkAsPaid();

        Assert.Throws<InvalidOperationException>(() => order.MarkAsPaid());
    }

    [Fact]
    public void MarkAsCompleted_WhenPaid_SetsStatusToCompletedAndRaisesEvent()
    {
        var order = Order.Domain.Aggregates.Order.Create("customer-1", CreateValidItems());
        order.MarkAsPaid();
        order.ClearDomainEvents();
        order.MarkAsCompleted();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Single(order.DomainEvents.OfType<OrderCompletedDomainEvent>());
    }

    [Fact]
    public void MarkAsCompleted_WhenNotPaid_ThrowsInvalidOperationException()
    {
        var order = Order.Domain.Aggregates.Order.Create("customer-1", CreateValidItems());
        Assert.Throws<InvalidOperationException>(() => order.MarkAsCompleted());
    }

    [Fact]
    public void Cancel_WhenPending_SetsStatusToCancelledAndRaisesEvent()
    {
        var order = Order.Domain.Aggregates.Order.Create("customer-1", CreateValidItems());
        order.Cancel("Customer requested");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        var evt = order.DomainEvents.OfType<OrderCancelledDomainEvent>().Single();
        Assert.Equal("Customer requested", evt.Reason);
    }

    [Fact]
    public void Cancel_WhenPaid_AllowsCancellation()
    {
        var order = Order.Domain.Aggregates.Order.Create("customer-1", CreateValidItems());
        order.MarkAsPaid();
        order.ClearDomainEvents();
        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsInvalidOperationException()
    {
        var order = Order.Domain.Aggregates.Order.Create("customer-1", CreateValidItems());
        order.MarkAsPaid();
        order.MarkAsCompleted();

        Assert.Throws<InvalidOperationException>(() => order.Cancel());
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_IsIdempotent()
    {
        var order = Order.Domain.Aggregates.Order.Create("customer-1", CreateValidItems());
        order.Cancel("First");
        order.ClearDomainEvents();
        order.Cancel("Second"); // Should not throw

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Empty(order.DomainEvents); // No new event on second cancel
    }
}
