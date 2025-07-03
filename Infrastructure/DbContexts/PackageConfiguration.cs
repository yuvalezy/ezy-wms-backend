using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        // Configure primary key
        builder.HasKey(p => p.Id);

        // Configure properties
        builder.Property(p => p.Barcode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(p => p.WhsCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.ClosedBy)
            .HasMaxLength(50);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.Property(p => p.CustomAttributes)
            .HasColumnType("NVARCHAR(MAX)");

        // Configure indexes
        builder.HasIndex(p => p.Barcode)
            .IsUnique()
            .HasDatabaseName("IX_Package_Barcode");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_Package_Status");

        builder.HasIndex(p => new { WhsCode = p.WhsCode, p.BinEntry })
            .HasDatabaseName("IX_Package_Location");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Package_CreatedAt");

        // Configure relationships
        builder.HasMany(p => p.Contents)
            .WithOne(c => c.Package)
            .HasForeignKey(c => c.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Transactions)
            .WithOne(t => t.Package)
            .HasForeignKey(t => t.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.LocationHistory)
            .WithOne(h => h.Package)
            .HasForeignKey(h => h.PackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PackageContentConfiguration : IEntityTypeConfiguration<PackageContent>
{
    public void Configure(EntityTypeBuilder<PackageContent> builder)
    {
        // Configure primary key
        builder.HasKey(c => c.Id);

        // Configure properties
        builder.Property(c => c.PackageId)
            .IsRequired();

        builder.Property(c => c.ItemCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Quantity)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(c => c.WhsCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.CreatedBy)
            .IsRequired()
            .HasMaxLength(50);

        // Configure indexes
        builder.HasIndex(c => c.PackageId)
            .HasDatabaseName("IX_PackageContent_Package");

        builder.HasIndex(c => c.ItemCode)
            .HasDatabaseName("IX_PackageContent_Item");

        builder.HasIndex(c => new { c.WhsCode, c.BinEntry })
            .HasDatabaseName("IX_PackageContent_Location");
    }
}

public class PackageTransactionConfiguration : IEntityTypeConfiguration<PackageTransaction>
{
    public void Configure(EntityTypeBuilder<PackageTransaction> builder)
    {
        // Configure primary key
        builder.HasKey(t => t.Id);

        // Configure properties
        builder.Property(t => t.PackageId)
            .IsRequired();

        builder.Property(t => t.TransactionType)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(t => t.ItemCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Quantity)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(t => t.UnitQuantity)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(t => t.UnitType)
            .IsRequired();

        builder.Property(t => t.SourceOperationType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.UserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.TransactionDate)
            .IsRequired();

        builder.Property(t => t.Notes)
            .HasMaxLength(500);

        // Configure indexes
        builder.HasIndex(t => t.PackageId)
            .HasDatabaseName("IX_PackageTransaction_Package");

        builder.HasIndex(t => t.TransactionDate)
            .HasDatabaseName("IX_PackageTransaction_Date");

        builder.HasIndex(t => new { t.SourceOperationType, t.SourceOperationId })
            .HasDatabaseName("IX_PackageTransaction_Operation");
    }
}

public class PackageLocationHistoryConfiguration : IEntityTypeConfiguration<PackageLocationHistory>
{
    public void Configure(EntityTypeBuilder<PackageLocationHistory> builder)
    {
        // Configure primary key
        builder.HasKey(h => h.Id);

        // Configure properties
        builder.Property(h => h.PackageId)
            .IsRequired();

        builder.Property(h => h.MovementType)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(h => h.FromWhsCode)
            .HasMaxLength(50);

        builder.Property(h => h.ToWhsCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(h => h.SourceOperationType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(h => h.UserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(h => h.MovementDate)
            .IsRequired();

        builder.Property(h => h.Notes)
            .HasMaxLength(500);

        // Configure indexes
        builder.HasIndex(h => h.PackageId)
            .HasDatabaseName("IX_PackageLocationHistory_Package");

        builder.HasIndex(h => h.MovementDate)
            .HasDatabaseName("IX_PackageLocationHistory_Date");
    }
}

public class PackageInconsistencyConfiguration : IEntityTypeConfiguration<PackageInconsistency>
{
    public void Configure(EntityTypeBuilder<PackageInconsistency> builder)
    {
        // Configure primary key
        builder.HasKey(i => i.Id);

        // Configure properties
        builder.Property(i => i.PackageId)
            .IsRequired();

        builder.Property(i => i.PackageBarcode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.ItemCode)
            .HasMaxLength(50);

        builder.Property(i => i.BatchNo)
            .HasMaxLength(50);

        builder.Property(i => i.SerialNo)
            .HasMaxLength(50);

        builder.Property(i => i.WhsCode)
            .HasMaxLength(50);

        builder.Property(i => i.SapQuantity)
            .HasPrecision(18, 6);

        builder.Property(i => i.WmsQuantity)
            .HasPrecision(18, 6);

        builder.Property(i => i.PackageQuantity)
            .HasPrecision(18, 6);

        builder.Property(i => i.InconsistencyType)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(i => i.Severity)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(i => i.DetectedAt)
            .IsRequired();

        builder.Property(i => i.ResolvedBy)
            .HasMaxLength(50);

        builder.Property(i => i.ResolutionAction)
            .HasMaxLength(500);

        builder.Property(i => i.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(i => i.Notes)
            .HasMaxLength(1000);

        // Configure indexes
        builder.HasIndex(i => i.PackageId)
            .HasDatabaseName("IX_PackageInconsistency_Package");

        builder.HasIndex(i => i.DetectedAt)
            .HasDatabaseName("IX_PackageInconsistency_DetectedAt");

        builder.HasIndex(i => i.IsResolved)
            .HasDatabaseName("IX_PackageInconsistency_IsResolved");

        builder.HasIndex(i => new { i.InconsistencyType, i.Severity })
            .HasDatabaseName("IX_PackageInconsistency_TypeSeverity");
    }
}