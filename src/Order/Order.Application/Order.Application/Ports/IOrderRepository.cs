using BuildingBlocks.SharedKernel;
using OrderAggregate = Order.Domain.Aggregates.Order;

namespace Order.Application.Ports;

using Order.Domain.Enums;

/// <summary>
/// Repository interface for order persistence.
/// This is a port in the hexagonal architecture - defines what the application needs,
/// not how it's implemented (that's in Infrastructure).
/// Extends the generic repository with order-specific methods if needed.
/// </summary>
public interface IOrderRepository : IRepository<OrderAggregate, Guid>
{
    /// <summary>
    /// Gets all orders with optional filtering.
    /// </summary>
    Task<IEnumerable<OrderAggregate>> GetOrdersAsync(
        string? customerId = null,
        OrderStatus? status = null,
        int? skip = null,
        int? take = null);
}

