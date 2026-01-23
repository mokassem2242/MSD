using Microsoft.EntityFrameworkCore;
using Order.Application.Ports;
using OrderAggregate = Order.Domain.Aggregates.Order;
using Order.Infrastructure.Persistence;

namespace Order.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IOrderRepository.
/// Handles persistence of Order aggregates to the database.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<OrderAggregate?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task AddAsync(OrderAggregate aggregate)
    {
        await _context.Orders.AddAsync(aggregate);
        // Domain events will be published automatically in SaveChangesAsync
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OrderAggregate aggregate)
    {
        _context.Orders.Update(aggregate);
        // Domain events will be published automatically in SaveChangesAsync
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await GetByIdAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<OrderAggregate>> GetOrdersAsync(
        string? customerId = null,
        Domain.Enums.OrderStatus? status = null,
        int? skip = null,
        int? take = null)
    {
        var query = _context.Orders
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(o => o.CustomerId == customerId);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (skip.HasValue)
        {
            query = query.Skip(skip.Value);
        }

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}

