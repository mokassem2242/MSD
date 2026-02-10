namespace Inventory.Domain.Entities;

/// <summary>
/// Entity representing a reservation of inventory for an order.
/// Tracks which products and quantities were reserved for a given order.
/// </summary>
public class Reservation
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public DateTime ReservedAt { get; private set; }
    public IReadOnlyList<ReservationLine> Lines => _lines.AsReadOnly();

    private List<ReservationLine> _lines = new();

    private Reservation() { }

    public Reservation(Guid id, Guid orderId, DateTime reservedAt, List<ReservationLine> lines)
    {
        Id = id;
        OrderId = orderId;
        ReservedAt = reservedAt;
        _lines.AddRange(lines ?? throw new ArgumentNullException(nameof(lines)));
    }

    public static Reservation Create(Guid orderId, List<(string ProductId, int Quantity)> items)
    {
        var id = Guid.NewGuid();
        var lines = items.Select(x => new ReservationLine(id, x.ProductId, x.Quantity)).ToList();
        return new Reservation(id, orderId, DateTime.UtcNow, lines);
    }
}

public class ReservationLine
{
    public Guid ReservationId { get; private set; }
    public string ProductId { get; private set; }
    public int Quantity { get; private set; }

    internal ReservationLine(Guid reservationId, string productId, int quantity)
    {
        ReservationId = reservationId;
        ProductId = productId;
        Quantity = quantity;
    }

    private ReservationLine() { ProductId = null!; }
}
