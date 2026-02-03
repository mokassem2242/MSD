namespace Order.Domain.Aggregates.Order.Domain.Enums;

/// <summary>
/// Represents the current status of an order in its lifecycle.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order has been created and is awaiting payment processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment has been successfully processed.
    /// </summary>
    Paid = 1,

    /// <summary>
    /// Order has been completed (payment succeeded and inventory reserved).
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Order has been cancelled (payment failed, inventory failed, or refunded).
    /// </summary>
    Cancelled = 3
}

