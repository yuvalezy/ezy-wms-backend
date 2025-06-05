using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt> {
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder) {
        // Configure auto-incrementing Number property
        builder.Property(e => e.Number)
            .UseIdentityColumn()
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // Configure Lines relationship with cascade delete
        builder
            .HasMany(gr => gr.Lines)
            .WithOne(grl => grl.GoodsReceipt)
            .HasForeignKey(grl => grl.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder
            .HasMany(gr => gr.Documents)
            .WithOne(grl => grl.GoodsReceipt)
            .HasForeignKey(grl => grl.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enums are now stored as integers by default
    }
}

public class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine> {
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder) {
        // Configure child relationships with cascade delete
        builder
            .HasMany(grl => grl.Targets)
            .WithOne(grt => grt.GoodsReceiptLine)
            .HasForeignKey(grt => grt.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(grl => grl.Sources)
            .WithOne(grs => grs.GoodsReceiptLine)
            .HasForeignKey(grs => grs.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enums are now stored as integers by default

        // Configure decimal precision
        builder.Property(e => e.Quantity)
            .HasPrecision(16, 6);
    }
}

public class GoodsReceiptTargetConfiguration : IEntityTypeConfiguration<GoodsReceiptTarget> {
    public void Configure(EntityTypeBuilder<GoodsReceiptTarget> builder) {
        // Enums are now stored as integers by default

        // Configure decimal precision
        builder.Property(e => e.TargetQuantity)
            .HasPrecision(16, 6);
    }
}

public class GoodsReceiptDocumentConfiguration : IEntityTypeConfiguration<GoodsReceiptDocument> {
    public void Configure(EntityTypeBuilder<GoodsReceiptDocument> builder) {
        // Configure child relationships with cascade delete
    }
}

public class GoodsReceiptSourceConfiguration : IEntityTypeConfiguration<GoodsReceiptSource> {
    public void Configure(EntityTypeBuilder<GoodsReceiptSource> builder) {
        // Configure decimal precision
        builder.Property(e => e.Quantity)
            .HasPrecision(16, 6);
    }
}