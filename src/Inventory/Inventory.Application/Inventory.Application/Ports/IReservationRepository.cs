using Inventory.Domain.Entities;

namespace Inventory.Application.Ports;

/// <summary>
/// Persists reservations for orders. Used for idempotency and audit.
/// </summary>
public interface IReservationRepository
{
    Task<Reservation?> GetByOrderIdAsync(Guid orderId);
    Task AddAsync(Reservation reservation);
}
