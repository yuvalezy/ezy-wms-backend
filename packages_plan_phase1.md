# Phase 1: Database Schema & Core Entities

## 1.1 Package Entity

```sql
CREATE TABLE Package (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Barcode NVARCHAR(50) NOT NULL UNIQUE,
    Status NVARCHAR(20) NOT NULL, -- Init, Active, Closed, Cancelled, Locked
    WhsCode NVARCHAR(50) NOT NULL,
    BinEntry INT NULL, -- NULL if warehouse doesn't support bins
    BinCode NVARCHAR(50) NULL,
    CreatedBy NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ClosedAt DATETIME2 NULL,
    ClosedBy NVARCHAR(50) NULL,
    Notes NVARCHAR(500) NULL,
    -- Audit fields (inherit from BaseEntity pattern)
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy NVARCHAR(50) NULL,
    -- Custom attributes (JSON field for flexibility)
    CustomAttributes NVARCHAR(MAX) NULL, -- JSON: {"attribute1": "value1", "attribute2": "value2"}
    
    INDEX IX_Package_Status (Status),
    INDEX IX_Package_Barcode (Barcode),
    INDEX IX_Package_Location (WhsCode, BinEntry),
    INDEX IX_Package_CreatedAt (CreatedAt)
);
```

## 1.2 PackageContent Entity

```sql
CREATE TABLE PackageContent (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PackageId UNIQUEIDENTIFIER NOT NULL,
    ItemCode NVARCHAR(50) NOT NULL,
    Quantity DECIMAL(18,6) NOT NULL,
    UnitCode NVARCHAR(10) NOT NULL,
    BatchNo NVARCHAR(50) NULL,
    SerialNo NVARCHAR(50) NULL,
    ExpiryDate DATE NULL,
    WhsCode NVARCHAR(50) NOT NULL, -- Must match Package.WhsCode
    BinEntry INT NULL, -- Must match Package.BinEntry
    BinCode NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(50) NOT NULL,
    
    FOREIGN KEY (PackageId) REFERENCES Package(Id) ON DELETE CASCADE,
    INDEX IX_PackageContent_Package (PackageId),
    INDEX IX_PackageContent_Item (ItemCode),
    INDEX IX_PackageContent_Location (WhsCode, BinEntry),
    
    -- Ensure location consistency
    CONSTRAINT CK_PackageContent_Location CHECK (
        -- Location must match parent package (enforced via trigger)
    )
);
```

## 1.3 PackageTransaction Entity (Content Movement History)

```sql
CREATE TABLE PackageTransaction (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PackageId UNIQUEIDENTIFIER NOT NULL,
    TransactionType NVARCHAR(20) NOT NULL, -- Add, Remove, Transfer, Count
    ItemCode NVARCHAR(50) NOT NULL,
    Quantity DECIMAL(18,6) NOT NULL, -- Positive for Add, Negative for Remove
    UnitCode NVARCHAR(10) NOT NULL,
    BatchNo NVARCHAR(50) NULL,
    SerialNo NVARCHAR(50) NULL,
    SourceOperationType NVARCHAR(20) NOT NULL, -- GoodsReceipt, Counting, Transfer, Picking
    SourceOperationId UNIQUEIDENTIFIER NULL, -- Reference to source operation
    SourceOperationLineId UNIQUEIDENTIFIER NULL,
    UserId NVARCHAR(50) NOT NULL,
    TransactionDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Notes NVARCHAR(500) NULL,
    
    FOREIGN KEY (PackageId) REFERENCES Package(Id),
    INDEX IX_PackageTransaction_Package (PackageId),
    INDEX IX_PackageTransaction_Date (TransactionDate),
    INDEX IX_PackageTransaction_Operation (SourceOperationType, SourceOperationId)
);
```

## 1.4 PackageLocationHistory Entity (Location Movement History)

```sql
CREATE TABLE PackageLocationHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PackageId UNIQUEIDENTIFIER NOT NULL,
    MovementType NVARCHAR(20) NOT NULL, -- Created, Moved, Transferred
    FromWhsCode NVARCHAR(50) NULL,
    FromBinEntry INT NULL,
    FromBinCode NVARCHAR(50) NULL,
    ToWhsCode NVARCHAR(50) NOT NULL,
    ToBinEntry INT NULL,
    ToBinCode NVARCHAR(50) NULL,
    SourceOperationType NVARCHAR(20) NOT NULL, -- GoodsReceipt, Transfer
    SourceOperationId UNIQUEIDENTIFIER NULL,
    UserId NVARCHAR(50) NOT NULL,
    MovementDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Notes NVARCHAR(500) NULL,
    
    FOREIGN KEY (PackageId) REFERENCES Package(Id),
    INDEX IX_PackageLocationHistory_Package (PackageId),
    INDEX IX_PackageLocationHistory_Date (MovementDate)
);
```

## 1.5 Database Constraints & Triggers

```sql
-- Trigger to ensure Package and PackageContent location consistency
CREATE TRIGGER TR_PackageContent_LocationSync
ON PackageContent
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN Package p ON i.PackageId = p.Id
        WHERE i.WhsCode != p.WhsCode OR ISNULL(i.BinEntry, 0) != ISNULL(p.BinEntry, 0)
    )
    BEGIN
        RAISERROR('Package content location must match package location', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;

-- Trigger to prevent operations on locked packages
CREATE TRIGGER TR_Package_LockValidation
ON PackageContent
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN Package p ON i.PackageId = p.Id
        WHERE p.Status = 'Locked'
    ) OR EXISTS (
        SELECT 1 FROM deleted d
        INNER JOIN Package p ON d.PackageId = p.Id
        WHERE p.Status = 'Locked'
    )
    BEGIN
        RAISERROR('Cannot modify locked package contents', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
```

## 1.6 Entity Framework Models

```csharp
public class Package : BaseEntity
{
    public Guid Id { get; set; }
    public string Barcode { get; set; }
    public PackageStatus Status { get; set; }
    public string WhsCode { get; set; }
    public int? BinEntry { get; set; }
    public string BinCode { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string ClosedBy { get; set; }
    public string Notes { get; set; }
    public string CustomAttributes { get; set; } // JSON string
    
    // Navigation properties
    public virtual ICollection<PackageContent> Contents { get; set; } = new List<PackageContent>();
    public virtual ICollection<PackageTransaction> Transactions { get; set; } = new List<PackageTransaction>();
    public virtual ICollection<PackageLocationHistory> LocationHistory { get; set; } = new List<PackageLocationHistory>();
}

public class PackageContent : BaseEntity
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public string ItemCode { get; set; }
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string WhsCode { get; set; }
    public int? BinEntry { get; set; }
    public string BinCode { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Package Package { get; set; }
}

public class PackageTransaction : BaseEntity
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public PackageTransactionType TransactionType { get; set; }
    public string ItemCode { get; set; }
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
    public string SourceOperationType { get; set; }
    public Guid? SourceOperationId { get; set; }
    public Guid? SourceOperationLineId { get; set; }
    public string UserId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Notes { get; set; }
    
    // Navigation properties
    public virtual Package Package { get; set; }
}

public class PackageLocationHistory : BaseEntity
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public PackageMovementType MovementType { get; set; }
    public string FromWhsCode { get; set; }
    public int? FromBinEntry { get; set; }
    public string FromBinCode { get; set; }
    public string ToWhsCode { get; set; }
    public int? ToBinEntry { get; set; }
    public string ToBinCode { get; set; }
    public string SourceOperationType { get; set; }
    public Guid? SourceOperationId { get; set; }
    public string UserId { get; set; }
    public DateTime MovementDate { get; set; }
    public string Notes { get; set; }
    
    // Navigation properties
    public virtual Package Package { get; set; }
}

public enum PackageStatus
{
    Init,
    Active,
    Closed,
    Cancelled,
    Locked
}

public enum PackageTransactionType
{
    Add,
    Remove,
    Transfer,
    Count
}

public enum PackageMovementType
{
    Created,
    Moved,
    Transferred
}
```

## 1.7 Entity Framework Configuration

```csharp
public class PackageEntityConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Barcode).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Status).IsRequired().HasConversion<string>();
        builder.Property(p => p.WhsCode).IsRequired().HasMaxLength(50);
        builder.Property(p => p.CreatedBy).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.CustomAttributes).HasColumnType("NVARCHAR(MAX)");
        
        builder.HasIndex(p => p.Barcode).IsUnique();
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => new { p.WhsCode, p.BinEntry });
        builder.HasIndex(p => p.CreatedAt);
        
        builder.HasMany(p => p.Contents)
            .WithOne(c => c.Package)
            .HasForeignKey(c => c.PackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PackageContentEntityConfiguration : IEntityTypeConfiguration<PackageContent>
{
    public void Configure(EntityTypeBuilder<PackageContent> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ItemCode).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Quantity).IsRequired().HasColumnType("DECIMAL(18,6)");
        builder.Property(c => c.UnitCode).IsRequired().HasMaxLength(10);
        builder.Property(c => c.WhsCode).IsRequired().HasMaxLength(50);
        builder.Property(c => c.CreatedBy).IsRequired().HasMaxLength(50);
        
        builder.HasIndex(c => c.PackageId);
        builder.HasIndex(c => c.ItemCode);
        builder.HasIndex(c => new { c.WhsCode, c.BinEntry });
    }
}
```

## 1.8 Migration Scripts

```csharp
public partial class AddPackageEntities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Package",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                WhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                BinEntry = table.Column<int>(type: "int", nullable: true),
                BinCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ClosedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                CustomAttributes = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Package", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PackageContent",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                UnitCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                BatchNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                SerialNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                WhsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                BinEntry = table.Column<int>(type: "int", nullable: true),
                BinCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PackageContent", x => x.Id);
                table.ForeignKey(
                    name: "FK_PackageContent_Package_PackageId",
                    column: x => x.PackageId,
                    principalTable: "Package",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create additional tables for PackageTransaction and PackageLocationHistory...
        
        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_Package_Barcode",
            table: "Package",
            column: "Barcode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Package_Status",
            table: "Package",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Package_Location",
            table: "Package",
            columns: new[] { "WhsCode", "BinEntry" });

        migrationBuilder.CreateIndex(
            name: "IX_Package_CreatedAt",
            table: "Package",
            column: "CreatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PackageLocationHistory");
        migrationBuilder.DropTable(name: "PackageTransaction");
        migrationBuilder.DropTable(name: "PackageContent");
        migrationBuilder.DropTable(name: "Package");
    }
}
```

## Implementation Notes

### Timeline: Week 1-2
- Database schema creation and migration scripts
- Base entity classes and EF Core configuration
- Core package entity interfaces

### Key Considerations
- All entities inherit from BaseEntity following existing patterns
- JSON custom attributes field for flexibility
- Comprehensive indexing for performance
- Location consistency enforced via triggers
- Package locking enforced at database level
- Cascade deletion for package contents

### Next Steps
- Phase 2: Implement package services and API controllers
- Integration with existing ILWDbContext
- Package barcode generation logic