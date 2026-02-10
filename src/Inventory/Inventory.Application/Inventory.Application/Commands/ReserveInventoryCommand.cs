namespace Inventory.Application.Commands;

public record ReserveInventoryCommand
{
    public Guid OrderId { get; init; }
    public IReadOnlyList<ReserveInventoryItem> Items { get; init; } = Array.Empty<ReserveInventoryItem>();
}

public record ReserveInventoryItem(string ProductId, int Quantity);
