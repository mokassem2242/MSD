using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("Id").IsRequired();
        builder.Property(x => x.OrderId).HasColumnName("OrderId").IsRequired();
        builder.Property(x => x.ReservedAt).HasColumnName("ReservedAt").IsRequired();

        builder.HasMany(r => r.Lines)
            .WithOne()
            .HasForeignKey(rl => rl.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Lines).HasField("_lines");

        builder.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("IX_Reservations_OrderId");
    }
}
