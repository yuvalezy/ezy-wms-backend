using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class InventoryCountingConfiguration : IEntityTypeConfiguration<InventoryCounting> {
    public void Configure(EntityTypeBuilder<InventoryCounting> builder) {
        // Configure auto-incrementing Number property
        builder.Property(e => e.Number)
            .UseIdentityColumn();

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
    }
}