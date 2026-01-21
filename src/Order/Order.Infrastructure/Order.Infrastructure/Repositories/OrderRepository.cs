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
}

