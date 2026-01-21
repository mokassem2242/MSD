using BuildingBlocks.SharedKernel;
using OrderAggregate = Order.Domain.Aggregates.Order;

namespace Order.Application.Ports;

/// <summary>
/// Repository interface for order persistence.
/// This is a port in the hexagonal architecture - defines what the application needs,
/// not how it's implemented (that's in Infrastructure).
/// Extends the generic repository with order-specific methods if needed.
/// </summary>
public interface IOrderRepository : IRepository<OrderAggregate, Guid>
{
    // Add order-specific methods here if needed, e.g.:
    // Task<OrderAggregate?> GetByCustomerIdAsync(string customerId);
    // Task<IEnumerable<OrderAggregate>> GetPendingOrdersAsync();
}

