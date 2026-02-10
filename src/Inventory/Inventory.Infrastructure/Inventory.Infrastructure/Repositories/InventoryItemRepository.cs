using Inventory.Application.Ports;
using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public class InventoryItemRepository : IInventoryItemRepository
{
    private readonly InventoryDbContext _context;

    public InventoryItemRepository(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id)
    {
        return await _context.InventoryItems.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<InventoryItem?> GetByProductIdAsync(string productId)
    {
        return await _context.InventoryItems.FirstOrDefaultAsync(x => x.ProductId == productId);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetByProductIdsAsync(IEnumerable<string> productIds)
    {
        var ids = productIds.ToList();
        if (ids.Count == 0) return Array.Empty<InventoryItem>();
        return await _context.InventoryItems
            .Where(x => ids.Contains(x.ProductId))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync()
    {
        return await _context.InventoryItems.OrderBy(x => x.ProductId).ToListAsync();
    }

    public async Task AddAsync(InventoryItem aggregate)
    {
        await _context.InventoryItems.AddAsync(aggregate);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(InventoryItem aggregate)
    {
        _context.InventoryItems.Update(aggregate);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await GetByIdAsync(id);
        if (item != null)
        {
            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
