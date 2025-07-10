using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class InventoryCountingConfiguration : IEntityTypeConfiguration<InventoryCounting> {
    public void Configure(EntityTypeBuilder<InventoryCounting> builder) {
        // Configure auto-incrementing Number property
        builder.Property(e => e.Number)
            .UseIdentityColumn()
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // Configure Lines relationship with cascade delete
        builder
            .HasMany(ic => ic.Lines)
            .WithOne(icl => icl.InventoryCounting)
            .HasForeignKey(icl => icl.InventoryCountingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure CountingPackages relationship with cascade delete
        builder
            .HasMany(ic => ic.CountingPackages)
            .WithOne(icp => icp.InventoryCounting)
            .HasForeignKey(icp => icp.InventoryCountingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enums are now stored as integers by default
    }
}

public class InventoryCountingLineConfiguration : IEntityTypeConfiguration<InventoryCountingLine> {
    public void Configure(EntityTypeBuilder<InventoryCountingLine> builder) {
    }
}