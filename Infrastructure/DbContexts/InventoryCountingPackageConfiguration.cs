using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class InventoryCountingPackageConfiguration : IEntityTypeConfiguration<InventoryCountingPackage> {
    public void Configure(EntityTypeBuilder<InventoryCountingPackage> builder) {
        // Configure relationships
        builder
            .HasOne(icp => icp.InventoryCounting)
            .WithMany(ic => ic.CountingPackages)
            .HasForeignKey(icp => icp.InventoryCountingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(icp => icp.Package)
            .WithMany()
            .HasForeignKey(icp => icp.PackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(icp => icp.Contents)
            .WithOne(icpc => icpc.InventoryCountingPackage)
            .HasForeignKey(icpc => icpc.InventoryCountingPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add unique constraint on InventoryCountingId + PackageId
        builder
            .HasIndex(icp => new { icp.InventoryCountingId, icp.PackageId })
            .IsUnique();
    }
}

public class InventoryCountingPackageContentConfiguration : IEntityTypeConfiguration<InventoryCountingPackageContent> {
    public void Configure(EntityTypeBuilder<InventoryCountingPackageContent> builder) {
        // Configure decimal precision
        builder.Property(e => e.CountedQuantity)
            .HasPrecision(18, 6);
            
        builder.Property(e => e.OriginalQuantity)
            .HasPrecision(18, 6);

        // Add index on InventoryCountingPackageId + ItemCode for performance
        builder
            .HasIndex(icpc => new { icpc.InventoryCountingPackageId, icpc.ItemCode });
    }
}