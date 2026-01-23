using Order.Application.Ports;
using Order.Application.Queries;
using OrderAggregate = Order.Domain.Aggregates.Order;

namespace Order.Application.Handlers;

/// <summary>
/// Handles the GetOrdersQuery.
/// </summary>
public class GetOrdersQueryHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    public async Task<List<OrderAggregate>> HandleAsync(GetOrdersQuery query)
    {
        var orders = await _orderRepository.GetOrdersAsync(
            query.CustomerId,
            query.Status,
            query.Skip,
            query.Take);

        return orders.ToList();
    }
}

