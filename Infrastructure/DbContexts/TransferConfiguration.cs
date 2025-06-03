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

        // Enums are now stored as integers by default
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

        // Enums are now stored as integers by default
    }
}
