using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class PickListConfiguration : IEntityTypeConfiguration<PickList> {
    public void Configure(EntityTypeBuilder<PickList> builder) {
        builder.Property(p => p.Quantity).HasPrecision(18, 2);
        builder.Property(p => p.ScannedQuantity).HasPrecision(18, 2);

        builder.HasOne(p => p.PickingPackageLabel)
            .WithMany(p => p.PickLists)
            .HasForeignKey(p => p.PickingPackageLabelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
