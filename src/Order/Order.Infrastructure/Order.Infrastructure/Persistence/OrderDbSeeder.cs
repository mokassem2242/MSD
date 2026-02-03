using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Domain.Aggregates.Order.Domain.Aggregates;
using Order.Domain.Aggregates.Order.Domain.ValueObjects;
using OrderAggregate = Order.Domain.Aggregates.Order.Domain.Aggregates.Order;

namespace Order.Domain.Aggregates.Order.Infrastructure.Persistence;

/// <summary>
/// Seeds the Order database with initial data for development and testing.
/// </summary>
public class OrderDbSeeder
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderDbSeeder> _logger;

    public OrderDbSeeder(OrderDbContext context, ILogger<OrderDbSeeder> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds the database with sample orders.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Check if database exists and can be connected to
            if (!await _context.Database.CanConnectAsync())
            {
                _logger.LogWarning("Cannot connect to database. Skipping seed.");
                return;
            }

            // Check if data already exists
            if (await _context.Orders.AnyAsync())
            {
                _logger.LogInformation("Database already contains orders. Skipping seed.");
                return;
            }

            _logger.LogInformation("Seeding Order database...");

            var orders = CreateSampleOrders();
            await _context.Orders.AddRangeAsync(orders);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} orders.", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private List<OrderAggregate> CreateSampleOrders()
    {
        var orders = new List<OrderAggregate>();

        // Order 1: Customer "CUST001" - Pending order
        var order1Items = new List<OrderItem>
        {
            new OrderItem("PROD001", 2, 29.99m),
            new OrderItem("PROD002", 1, 49.99m)
        };
        var order1 = OrderAggregate.Create("CUST001", order1Items);
        orders.Add(order1);

        // Order 2: Customer "CUST002" - Paid order
        var order2Items = new List<OrderItem>
        {
            new OrderItem("PROD003", 3, 15.50m),
            new OrderItem("PROD004", 2, 99.99m)
        };
        var order2 = OrderAggregate.Create("CUST002", order2Items);
        order2.MarkAsPaid(); // Mark as paid
        orders.Add(order2);

        // Order 3: Customer "CUST001" - Completed order
        var order3Items = new List<OrderItem>
        {
            new OrderItem("PROD005", 1, 199.99m)
        };
        var order3 = OrderAggregate.Create("CUST001", order3Items);
        order3.MarkAsPaid();
        order3.MarkAsCompleted(); // Mark as completed
        orders.Add(order3);

        // Order 4: Customer "CUST003" - Cancelled order
        var order4Items = new List<OrderItem>
        {
            new OrderItem("PROD006", 5, 9.99m),
            new OrderItem("PROD007", 2, 24.99m)
        };
        var order4 = OrderAggregate.Create("CUST003", order4Items);
        order4.Cancel("Customer requested cancellation");
        orders.Add(order4);

        // Order 5: Customer "CUST002" - Large order
        var order5Items = new List<OrderItem>
        {
            new OrderItem("PROD001", 10, 29.99m),
            new OrderItem("PROD002", 5, 49.99m),
            new OrderItem("PROD003", 8, 15.50m)
        };
        var order5 = OrderAggregate.Create("CUST002", order5Items);
        orders.Add(order5);

        // Order 6: Customer "CUST004" - Single item order
        var order6Items = new List<OrderItem>
        {
            new OrderItem("PROD008", 1, 79.99m)
        };
        var order6 = OrderAggregate.Create("CUST004", order6Items);
        order6.MarkAsPaid();
        orders.Add(order6);

        return orders;
    }
}

