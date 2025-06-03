using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class CancellationReasonConfiguration : IEntityTypeConfiguration<CancellationReason> {
    public void Configure(EntityTypeBuilder<CancellationReason> builder) {
        builder.ToTable("CancellationReasons");
        
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);
            
        builder.Property(e => e.Transfer)
            .HasDefaultValue(false);
            
        builder.Property(e => e.GoodsReceipt)
            .HasDefaultValue(false);
            
        builder.Property(e => e.Counting)
            .HasDefaultValue(false);
            
        // Index for faster lookups by object type
        builder.HasIndex(e => new { e.Transfer, e.GoodsReceipt, e.Counting, e.IsEnabled })
            .HasDatabaseName("IX_CancellationReasons_ObjectTypes");
    }
}