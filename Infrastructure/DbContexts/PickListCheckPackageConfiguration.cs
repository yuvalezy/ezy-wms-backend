using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class PickListCheckPackageConfiguration : IEntityTypeConfiguration<PickListCheckPackage> {
    public void Configure(EntityTypeBuilder<PickListCheckPackage> builder) {
        builder.Property(p => p.PackageBarcode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.CheckedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(p => p.CheckSessionId)
            .HasDatabaseName("IX_PickListCheckPackage_CheckSessionId");

        builder.HasIndex(p => p.PackageId)
            .HasDatabaseName("IX_PickListCheckPackage_PackageId");

        builder.HasIndex(p => p.CheckedAt)
            .HasDatabaseName("IX_PickListCheckPackage_CheckedAt");

        // Unique constraint to ensure a package can only be scanned once per session
        builder.HasIndex(p => new { p.CheckSessionId, p.PackageId })
            .HasDatabaseName("IX_PickListCheckPackage_CheckSessionId_PackageId")
            .IsUnique();

        // Relationships
        builder.HasOne(p => p.CheckSession)
            .WithMany(s => s.CheckedPackages)
            .HasForeignKey(p => p.CheckSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Package)
            .WithMany()
            .HasForeignKey(p => p.PackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CheckedByUser)
            .WithMany()
            .HasForeignKey(p => p.CheckedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}