using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderAggregate = Order.Domain.Aggregates.Order;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;

namespace Order.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Order aggregate.
/// Maps the domain model to the database schema.
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<OrderAggregate>
{
    public void Configure(EntityTypeBuilder<OrderAggregate> builder)
    {
        // Configure table name
        builder.ToTable("Orders");

        // Configure primary key
        builder.HasKey(o => o.Id);

        // Configure properties
        builder.Property(o => o.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(o => o.CustomerId)
            .HasColumnName("CustomerId")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("Status")
            .HasConversion(
                v => v.ToString(),
                v => (OrderStatus)Enum.Parse(typeof(OrderStatus), v))
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasColumnName("TotalAmount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        // Configure owned type (OrderItem as value object)
        builder.OwnsMany(o => o.OrderItems, orderItem =>
        {
            orderItem.ToTable("OrderItems");

            orderItem.WithOwner()
                .HasForeignKey("OrderId");

            orderItem.Property<Guid>("Id")
                .ValueGeneratedOnAdd();

            orderItem.HasKey("Id");

            orderItem.Property(oi => oi.ProductId)
                .HasColumnName("ProductId")
                .HasMaxLength(100)
                .IsRequired();

            orderItem.Property(oi => oi.Quantity)
                .HasColumnName("Quantity")
                .IsRequired();

            orderItem.Property(oi => oi.Price)
                .HasColumnName("Price")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        // Ignore domain events (they are not persisted)
        builder.Ignore(o => o.DomainEvents);
    }
}

