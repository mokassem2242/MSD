using Order.Domain.Aggregates.Order.Api.DTOs;
using OrderAggregate = Order.Domain.Aggregates.Order.Domain.Aggregates.Order;

namespace Order.Domain.Aggregates.Order.Api.Mappings;

/// <summary>
/// Extension methods for mapping domain models to DTOs.
/// </summary>
public static class OrderMappings
{
    public static OrderResponse ToResponse(this OrderAggregate order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Items = order.OrderItems.Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price,
                TotalPrice = item.GetTotalPrice()
            }).ToList(),
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        };
    }

    public static List<OrderResponse> ToResponseList(this IEnumerable<OrderAggregate> orders)
    {
        return orders.Select(ToResponse).ToList();
    }
}

