using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Seeds the inventory database with sample product stock for development.
/// </summary>
public class InventoryDbSeeder
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<InventoryDbSeeder> _logger;

    public InventoryDbSeeder(InventoryDbContext context, ILogger<InventoryDbSeeder> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        if (await _context.InventoryItems.AnyAsync())
        {
            _logger.LogInformation("Inventory already has data, skipping seed.");
            return;
        }

        var items = new[]
        {
            InventoryItem.Create("product-1", 100),
            InventoryItem.Create("product-2", 50),
            InventoryItem.Create("product-3", 25),
            InventoryItem.Create("widget-a", 200),
            InventoryItem.Create("widget-b", 75)
        };

        await _context.InventoryItems.AddRangeAsync(items);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} inventory items.", items.Length);
    }
}
