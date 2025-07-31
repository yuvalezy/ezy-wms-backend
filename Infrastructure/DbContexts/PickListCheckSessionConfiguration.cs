using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class PickListCheckSessionConfiguration : IEntityTypeConfiguration<PickListCheckSession> {
    public void Configure(EntityTypeBuilder<PickListCheckSession> builder) {
        builder.Property(p => p.StartedByUserName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.StartedAt)
            .IsRequired();

        builder.Property(p => p.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.IsCancelled)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(p => p.PickListId)
            .HasDatabaseName("IX_PickListCheckSession_PickListId");

        builder.HasIndex(p => p.StartedAt)
            .HasDatabaseName("IX_PickListCheckSession_StartedAt");

        builder.HasIndex(p => p.IsCompleted)
            .HasDatabaseName("IX_PickListCheckSession_IsCompleted");

        builder.HasIndex(p => new { p.PickListId, p.IsCompleted })
            .HasDatabaseName("IX_PickListCheckSession_PickListId_IsCompleted");

        // Relationships
        builder.HasOne(p => p.StartedByUser)
            .WithMany()
            .HasForeignKey(p => p.StartedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.CheckedItems)
            .WithOne(i => i.CheckSession)
            .HasForeignKey(i => i.CheckSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PickListCheckItemConfiguration : IEntityTypeConfiguration<PickListCheckItem> {
    public void Configure(EntityTypeBuilder<PickListCheckItem> builder) {
        builder.Property(p => p.ItemCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.CheckedQuantity)
            .IsRequired();

        builder.Property(p => p.Unit)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.CheckedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(p => p.CheckSessionId)
            .HasDatabaseName("IX_PickListCheckItem_CheckSessionId");

        builder.HasIndex(p => p.ItemCode)
            .HasDatabaseName("IX_PickListCheckItem_ItemCode");

        builder.HasIndex(p => p.CheckedAt)
            .HasDatabaseName("IX_PickListCheckItem_CheckedAt");

        builder.HasIndex(p => new { p.CheckSessionId, p.ItemCode })
            .HasDatabaseName("IX_PickListCheckItem_CheckSessionId_ItemCode");

        // Relationships
        builder.HasOne(p => p.CheckSession)
            .WithMany(s => s.CheckedItems)
            .HasForeignKey(p => p.CheckSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CheckedByUser)
            .WithMany()
            .HasForeignKey(p => p.CheckedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}