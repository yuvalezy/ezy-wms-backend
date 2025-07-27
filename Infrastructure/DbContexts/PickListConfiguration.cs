using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class PickListConfiguration : IEntityTypeConfiguration<PickList> {
    public void Configure(EntityTypeBuilder<PickList> builder) {
        // Enums are now stored as integers by default
    }
}

public class PickListPackageConfiguration : IEntityTypeConfiguration<PickListPackage> {
    public void Configure(EntityTypeBuilder<PickListPackage> builder) {
        // Use the inherited Id as primary key since PickEntry can be nullable
        builder.HasKey(p => p.Id);

        // Configure properties
        builder.Property(p => p.AbsEntry)
            .IsRequired();

        builder.Property(p => p.PickEntry)
            .IsRequired(false); // Will be validated conditionally based on Type

        builder.Property(p => p.PackageId)
            .IsRequired();

        builder.Property(p => p.Type)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(p => p.AddedAt)
            .IsRequired();

        builder.Property(p => p.AddedByUserId)
            .IsRequired();

        // Create unique index for Source packages (when PickEntry is not null)
        builder.HasIndex(p => new { p.AbsEntry, p.PickEntry, p.PackageId, p.Type })
            .HasDatabaseName("IX_PickListPackage_Unique_Source")
            .IsUnique()
            .HasFilter("[PickEntry] IS NOT NULL");

        // Create unique index for Target packages (when PickEntry is null)
        builder.HasIndex(p => new { p.AbsEntry, p.PackageId, p.Type })
            .HasDatabaseName("IX_PickListPackage_Unique_Target")
            .IsUnique()
            .HasFilter("[PickEntry] IS NULL");

        builder.HasIndex(p => p.PackageId)
            .HasDatabaseName("IX_PickListPackage_Package");

        builder.HasIndex(p => p.Type)
            .HasDatabaseName("IX_PickListPackage_Type");

        builder.HasIndex(p => p.AddedAt)
            .HasDatabaseName("IX_PickListPackage_AddedAt");

        // Add check constraint: PickEntry is required when Type = Source (0)
        builder.HasCheckConstraint("CK_PickListPackage_PickEntry_Required_For_Source", 
            "([Type] != 0 OR [PickEntry] IS NOT NULL)");

        // Configure relationships
        builder.HasOne(p => p.Package)
            .WithMany()
            .HasForeignKey(p => p.PackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}