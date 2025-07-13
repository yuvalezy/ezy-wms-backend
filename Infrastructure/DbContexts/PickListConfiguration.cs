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
        // Configure composite primary key
        builder.HasKey(p => new { p.AbsEntry, p.PickEntry, p.PackageId, p.Type });

        // Configure properties
        builder.Property(p => p.AbsEntry)
            .IsRequired();

        builder.Property(p => p.PickEntry)
            .IsRequired();

        builder.Property(p => p.PackageId)
            .IsRequired();

        builder.Property(p => p.Type)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(p => p.AddedAt)
            .IsRequired();

        builder.Property(p => p.AddedByUserId)
            .IsRequired();

        // Configure indexes
        builder.HasIndex(p => new { p.AbsEntry, p.PickEntry })
            .HasDatabaseName("IX_PickListPackage_Operation");

        builder.HasIndex(p => p.PackageId)
            .HasDatabaseName("IX_PickListPackage_Package");

        builder.HasIndex(p => p.Type)
            .HasDatabaseName("IX_PickListPackage_Type");

        builder.HasIndex(p => p.AddedAt)
            .HasDatabaseName("IX_PickListPackage_AddedAt");

        // Configure relationships
        builder.HasOne(p => p.Package)
            .WithMany()
            .HasForeignKey(p => p.PackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}