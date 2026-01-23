namespace Order.Api.DTOs;

/// <summary>
/// Request DTO for cancelling an order.
/// </summary>
public class CancelOrderRequest
{
    public string? Reason { get; init; }
}

