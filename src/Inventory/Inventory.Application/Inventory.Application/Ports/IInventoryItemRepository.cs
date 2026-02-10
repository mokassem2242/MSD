using BuildingBlocks.SharedKernel;
using Inventory.Domain.Aggregates;

namespace Inventory.Application.Ports;

/// <summary>
/// Repository for inventory items. Used for reserve/release operations.
/// </summary>
public interface IInventoryItemRepository : IRepository<InventoryItem, Guid>
{
    Task<InventoryItem?> GetByProductIdAsync(string productId);

    Task<IReadOnlyList<InventoryItem>> GetByProductIdsAsync(IEnumerable<string> productIds);

    Task<IReadOnlyList<InventoryItem>> GetAllAsync();
}
