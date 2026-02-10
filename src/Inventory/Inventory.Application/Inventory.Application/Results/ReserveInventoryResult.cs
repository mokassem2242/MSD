namespace Inventory.Application.Results;

public abstract record ReserveInventoryResult;

public sealed record ReserveInventorySuccess(Guid ReservationId, IReadOnlyList<ReservedItem> Items, DateTime ReservedAt) : ReserveInventoryResult;

public sealed record ReservedItem(string ProductId, int Quantity);

public sealed record ReserveInventoryFailure(string Reason, IReadOnlyList<FailedItem> FailedItems) : ReserveInventoryResult;

public sealed record FailedItem(string ProductId, int RequestedQuantity, int AvailableQuantity);
