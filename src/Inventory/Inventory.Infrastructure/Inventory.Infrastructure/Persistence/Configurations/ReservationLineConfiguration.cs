using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class ReservationLineConfiguration : IEntityTypeConfiguration<ReservationLine>
{
    public void Configure(EntityTypeBuilder<ReservationLine> builder)
    {
        builder.ToTable("ReservationLines");

        builder.HasKey(x => new { x.ReservationId, x.ProductId });

        builder.Property(x => x.ReservationId).HasColumnName("ReservationId").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("ProductId").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("Quantity").IsRequired();
    }
}
