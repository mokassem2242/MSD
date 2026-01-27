using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Domain.Aggregates;
using PaymentAggregate = Payment.Domain.Aggregates.Payment;

namespace Payment.Infrastructure.Persistence;

/// <summary>
/// Seeds the Payment database with initial data for development and testing.
/// </summary>
public class PaymentDbSeeder
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentDbSeeder> _logger;

    public PaymentDbSeeder(PaymentDbContext context, ILogger<PaymentDbSeeder> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds the database with sample payments.
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
            if (await _context.Payments.AnyAsync())
            {
                _logger.LogInformation("Database already contains payments. Skipping seed.");
                return;
            }

            _logger.LogInformation("Seeding Payment database...");

            var payments = CreateSamplePayments();
            await _context.Payments.AddRangeAsync(payments);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} payments.", payments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private List<PaymentAggregate> CreateSamplePayments()
    {
        var payments = new List<PaymentAggregate>();

        // Payment 1: Succeeded payment for OrderId (will be a random GUID for demo)
        var payment1 = PaymentAggregate.Create(
            Guid.NewGuid(),
            "CUST001",
            109.97m); // 2 * 29.99 + 1 * 49.99
        payment1.MarkAsSucceeded();
        payments.Add(payment1);

        // Payment 2: Failed payment
        var payment2 = PaymentAggregate.Create(
            Guid.NewGuid(),
            "CUST002",
            259.97m); // 3 * 15.50 + 2 * 99.99
        payment2.MarkAsFailed("Insufficient funds");
        payments.Add(payment2);

        // Payment 3: Succeeded payment
        var payment3 = PaymentAggregate.Create(
            Guid.NewGuid(),
            "CUST001",
            199.99m);
        payment3.MarkAsSucceeded();
        payments.Add(payment3);

        // Payment 4: Pending payment
        var payment4 = PaymentAggregate.Create(
            Guid.NewGuid(),
            "CUST003",
            74.93m); // 5 * 9.99 + 2 * 24.99
        // Leave as pending
        payments.Add(payment4);

        // Payment 5: Refunded payment
        var payment5 = PaymentAggregate.Create(
            Guid.NewGuid(),
            "CUST002",
            599.40m); // 10 * 29.99 + 5 * 49.99 + 8 * 15.50
        payment5.MarkAsSucceeded();
        payment5.Refund();
        payments.Add(payment5);

        return payments;
    }
}
