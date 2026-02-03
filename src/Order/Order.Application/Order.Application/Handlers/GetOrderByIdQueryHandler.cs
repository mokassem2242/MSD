using Order.Domain.Aggregates.Order.Application.Ports;
using Order.Domain.Aggregates.Order.Application.Queries;
using OrderAggregate = Order.Domain.Aggregates.Order.Domain.Aggregates.Order;

namespace Order.Domain.Aggregates.Order.Application.Handlers;

/// <summary>
/// Handles the GetOrderByIdQuery.
/// </summary>
public class GetOrderByIdQueryHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    public async Task<OrderAggregate?> HandleAsync(GetOrderByIdQuery query)
    {
        return await _orderRepository.GetByIdAsync(query.OrderId);
    }
}

