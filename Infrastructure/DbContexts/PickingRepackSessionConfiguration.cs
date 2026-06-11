using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class PickingRepackSessionConfiguration : IEntityTypeConfiguration<PickingRepackSession> {
    public void Configure(EntityTypeBuilder<PickingRepackSession> builder) {
        builder.Property(p => p.WhsCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.StartedByUserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => new { p.AbsEntry, p.WhsCode })
            .HasDatabaseName("IX_PickingRepackSession_PickList");

        builder.HasIndex(p => new { p.AbsEntry, p.WhsCode, p.IsCompleted, p.IsCancelled })
            .HasDatabaseName("IX_PickingRepackSession_Status");
    }
}
