using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer> {
    public void Configure(EntityTypeBuilder<Transfer> builder) {
        // Configure StatusUser relationship
        builder
            .HasOne(t => t.StatusUser)
            .WithMany()
            .HasForeignKey(t => t.StatusUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Lines relationship with cascade delete
        builder
            .HasMany(t => t.Lines)
            .WithOne(tl => tl.Transfer)
            .HasForeignKey(tl => tl.TransferId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure enum conversion for char storage
        builder.Property(e => e.Status)
            .HasConversion<char>();
    }
}
public class TransferLineConfiguration : IEntityTypeConfiguration<TransferLine> {
    public void Configure(EntityTypeBuilder<TransferLine> builder) {
        // Configure StatusUser relationship
        builder
            .HasOne(tl => tl.StatusUser)
            .WithMany()
            .HasForeignKey(tl => tl.StatusUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure enum conversions
        builder.Property(e => e.LineStatus)
            .HasConversion<char>();
            
        builder.Property(e => e.Type)
            .HasConversion<char>();
            
        builder.Property(e => e.UnitType)
            .HasConversion<int>();
    }
}
