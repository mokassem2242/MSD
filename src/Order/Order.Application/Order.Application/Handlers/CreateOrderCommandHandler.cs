using Order.Domain.Aggregates.Order.Application.Commands;
using Order.Domain.Aggregates.Order.Application.Ports;
using OrderAggregate = Order.Domain.Aggregates.Order.Domain.Aggregates.Order;
using Order.Domain.Aggregates.Order.Domain.ValueObjects;

namespace Order.Domain.Aggregates.Order.Application.Handlers;

/// <summary>
/// Handles the CreateOrderCommand by creating an order aggregate and persisting it.
/// Domain events are automatically published in SaveChangesAsync.
/// </summary>
public class CreateOrderCommandHandler
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    /// <summary>
    /// Handles the create order command.
    /// </summary>
    /// <param name="command">The command containing order details</param>
    /// <returns>The ID of the created order</returns>
    public async Task<Guid> HandleAsync(CreateOrderCommand command)
    {
        // Validate command
        if (string.IsNullOrWhiteSpace(command.CustomerId))
            throw new ArgumentException("CustomerId is required", nameof(command));

        if (command.Items == null || command.Items.Count == 0)
            throw new ArgumentException("Order must have at least one item", nameof(command));

        // Convert command items to domain value objects
        var orderItems = command.Items.Select(item =>
            new OrderItem(item.ProductId, item.Quantity, item.Price)
        ).ToList();

        // Create order aggregate using factory method
        var order = OrderAggregate.Create(command.CustomerId, orderItems);

        // Save to repository
        // Domain events will be automatically published in SaveChangesAsync
        await _orderRepository.AddAsync(order);

        return order.Id;
    }
}

