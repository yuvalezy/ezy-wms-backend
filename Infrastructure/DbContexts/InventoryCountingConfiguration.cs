using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class InventoryCountingConfiguration : IEntityTypeConfiguration<InventoryCounting> {
    public void Configure(EntityTypeBuilder<InventoryCounting> builder) {
        // Configure StatusUser relationship
        builder
            .HasOne(ic => ic.StatusUser)
            .WithMany()
            .HasForeignKey(ic => ic.StatusUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Lines relationship with cascade delete
        builder
            .HasMany(ic => ic.Lines)
            .WithOne(icl => icl.InventoryCounting)
            .HasForeignKey(icl => icl.InventoryCountingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enums are now stored as integers by default
    }
}

public class InventoryCountingLineConfiguration : IEntityTypeConfiguration<InventoryCountingLine> {
    public void Configure(EntityTypeBuilder<InventoryCountingLine> builder) {
        // Configure StatusUser relationship
        builder
            .HasOne(icl => icl.StatusUser)
            .WithMany()
            .HasForeignKey(icl => icl.StatusUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enums are now stored as integers by default
    }
}