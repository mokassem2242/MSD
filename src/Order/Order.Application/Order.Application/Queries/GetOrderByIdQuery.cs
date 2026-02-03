namespace Order.Domain.Aggregates.Order.Application.Queries;

/// <summary>
/// Query to get an order by its ID.
/// </summary>
public class GetOrderByIdQuery
{
    public Guid OrderId { get; init; }

    public GetOrderByIdQuery(Guid orderId)
    {
        OrderId = orderId;
    }
}

