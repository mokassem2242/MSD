using Order.Domain.Aggregates.Order.Application.Commands;
using Order.Domain.Aggregates.Order.Application.Ports;
using OrderAggregate = Order.Domain.Aggregates.Order.Domain.Aggregates.Order;

namespace Order.Domain.Aggregates.Order.Application.Handlers;

/// <summary>
/// Handles the CancelOrderCommand.
/// </summary>
public class CancelOrderCommandHandler
{
    private readonly IOrderRepository _orderRepository;

    public CancelOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    public async Task HandleAsync(CancelOrderCommand command)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId);
        
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {command.OrderId} not found");
        }

        order.Cancel(command.Reason);
        await _orderRepository.UpdateAsync(order);
    }
}

