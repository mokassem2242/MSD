using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentAggregate = Payment.Domain.Aggregates.Payment;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Payment aggregate.
/// Maps the domain model to the database schema.
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<PaymentAggregate>
{
    public void Configure(EntityTypeBuilder<PaymentAggregate> builder)
    {
        // Configure table name
        builder.ToTable("Payments");

        // Configure primary key
        builder.HasKey(p => p.Id);

        // Configure properties
        builder.Property(p => p.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(p => p.OrderId)
            .HasColumnName("OrderId")
            .IsRequired();

        builder.Property(p => p.CustomerId)
            .HasColumnName("CustomerId")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnName("Amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("Status")
            .HasConversion(
                v => v.ToString(),
                v => (PaymentStatus)Enum.Parse(typeof(PaymentStatus), v))
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(p => p.ProcessedAt)
            .HasColumnName("ProcessedAt")
            .IsRequired(false);

        builder.Property(p => p.FailureReason)
            .HasColumnName("FailureReason")
            .HasMaxLength(500)
            .IsRequired(false);

        // Create index on OrderId for faster lookups (idempotency checks)
        builder.HasIndex(p => p.OrderId)
            .HasDatabaseName("IX_Payments_OrderId");

        // Ignore domain events (they are not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
}
