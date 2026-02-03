using Order.Domain.Aggregates.Order.Domain.Enums;

namespace Order.Domain.Aggregates.Order.Application.Queries;

/// <summary>
/// Query to get orders with optional filtering.
/// </summary>
public class GetOrdersQuery
{
    public string? CustomerId { get; init; }
    public OrderStatus? Status { get; init; }
    public int? Skip { get; init; }
    public int? Take { get; init; }

    public GetOrdersQuery(string? customerId = null, OrderStatus? status = null, int? skip = null, int? take = null)
    {
        CustomerId = customerId;
        Status = status;
        Skip = skip;
        Take = take;
    }
}

