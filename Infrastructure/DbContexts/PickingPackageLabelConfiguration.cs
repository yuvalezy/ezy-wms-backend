using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class PickingPackageLabelConfiguration : IEntityTypeConfiguration<PickingPackageLabel> {
    public void Configure(EntityTypeBuilder<PickingPackageLabel> builder) {
        builder.Property(p => p.WhsCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(p => new { p.AbsEntry, p.WhsCode, p.Sequence })
            .IsUnique()
            .HasFilter("[Deleted] = 0")
            .HasDatabaseName("IX_PickingPackageLabel_Unique_Sequence");

        builder.HasIndex(p => new { p.AbsEntry, p.WhsCode, p.Code })
            .IsUnique()
            .HasFilter("[Deleted] = 0")
            .HasDatabaseName("IX_PickingPackageLabel_Unique_Code");
    }
}
