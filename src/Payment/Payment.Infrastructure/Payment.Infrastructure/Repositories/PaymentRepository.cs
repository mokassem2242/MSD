using Microsoft.EntityFrameworkCore;
using Payment.Application.Ports;
using Payment.Infrastructure.Persistence;
using PaymentAggregate = Payment.Domain.Aggregates.Payment;

namespace Payment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for payment persistence.
/// Implements IPaymentRepository using Entity Framework Core.
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PaymentAggregate?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<PaymentAggregate>> GetAllAsync()
    {
        return await _context.Payments
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaymentAggregate?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task AddAsync(PaymentAggregate entity)
    {
        await _context.Payments.AddAsync(entity);
        // Domain events will be published automatically in SaveChangesAsync
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PaymentAggregate entity)
    {
        _context.Payments.Update(entity);
        // Domain events will be published automatically in SaveChangesAsync
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var payment = await GetByIdAsync(id);
        if (payment != null)
        {
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
        }
    }
}
