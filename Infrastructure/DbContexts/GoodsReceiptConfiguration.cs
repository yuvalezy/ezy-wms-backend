using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt> {
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder) {
        // Configure StatusUser relationship
        builder
            .HasOne(gr => gr.StatusUser)
            .WithMany()
            .HasForeignKey(gr => gr.StatusUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Lines relationship with cascade delete
        builder
            .HasMany(gr => gr.Lines)
            .WithOne(grl => grl.GoodsReceipt)
            .HasForeignKey(grl => grl.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure enum conversion for char storage
        builder.Property(e => e.Status)
            .HasConversion<char>();

        builder.Property(e => e.Type)
            .HasConversion<char>();
    }
}

public class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine> {
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder) {
        // Configure StatusUser relationship
        builder
            .HasOne(grl => grl.StatusUser)
            .WithMany()
            .HasForeignKey(grl => grl.StatusUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure child relationships with cascade delete
        builder
            .HasMany(grl => grl.Targets)
            .WithOne(grt => grt.GoodsReceiptLine)
            .HasForeignKey(grt => grt.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(grl => grl.Documents)
            .WithOne(grd => grd.GoodsReceiptLine)
            .HasForeignKey(grd => grd.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(grl => grl.Sources)
            .WithOne(grs => grs.GoodsReceiptLine)
            .HasForeignKey(grs => grs.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure enum conversions
        builder.Property(e => e.LineStatus)
            .HasConversion<char>();

        builder.Property(e => e.Unit)
            .HasConversion<int>();

        // Configure decimal precision
        builder.Property(e => e.Quantity)
            .HasPrecision(16, 6);
    }
}

public class GoodsReceiptTargetConfiguration : IEntityTypeConfiguration<GoodsReceiptTarget> {
    public void Configure(EntityTypeBuilder<GoodsReceiptTarget> builder) {
        // Configure enum conversion
        builder.Property(e => e.TargetStatus)
            .HasConversion<char>();

        // Configure decimal precision
        builder.Property(e => e.TargetQuantity)
            .HasPrecision(16, 6);
    }
}

public class GoodsReceiptDocumentConfiguration : IEntityTypeConfiguration<GoodsReceiptDocument> {
    public void Configure(EntityTypeBuilder<GoodsReceiptDocument> builder) {
        // No additional configuration needed beyond what's in annotations
    }
}

public class GoodsReceiptSourceConfiguration : IEntityTypeConfiguration<GoodsReceiptSource> {
    public void Configure(EntityTypeBuilder<GoodsReceiptSource> builder) {
        // Configure decimal precision
        builder.Property(e => e.Quantity)
            .HasPrecision(16, 6);
    }
}