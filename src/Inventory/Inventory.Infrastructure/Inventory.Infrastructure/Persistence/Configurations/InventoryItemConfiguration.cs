using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("Id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("ProductId").HasMaxLength(100).IsRequired();
        builder.Property(x => x.QuantityInStock).HasColumnName("QuantityInStock").IsRequired();
        builder.Property(x => x.QuantityReserved).HasColumnName("QuantityReserved").IsRequired();

        builder.HasIndex(x => x.ProductId).IsUnique().HasDatabaseName("IX_InventoryItems_ProductId");
        builder.Ignore(x => x.DomainEvents);
    }
}
