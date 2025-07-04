# License System Database Entities Summary

All entities inherit from `BaseEntity` and are ready for database migration.

## Phase 1 Entities

### Device
```csharp
using Core.Entities;

public class Device : BaseEntity
{
    public string DeviceUuid { get; set; } // From frontend localStorage
    public string DeviceName { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DeviceStatus Status { get; set; }
    public string StatusNotes { get; set; }
    public DateTime LastActiveDate { get; set; }
}

public enum DeviceStatus
{
    Active = 1,
    Inactive = 2,
    Disabled = 3
}
```

### DeviceAudit
```csharp
using Core.Entities;

public class DeviceAudit : BaseEntity
{
    public Guid DeviceId { get; set; }
    public DeviceStatus PreviousStatus { get; set; }
    public DeviceStatus NewStatus { get; set; }
    public string Reason { get; set; }
    
    public virtual Device Device { get; set; }
}
```

## Phase 2 Entities

### AccountStatus
```csharp
using Core.Entities;

public class AccountStatus : BaseEntity
{
    public AccountState Status { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? PaymentCycleDate { get; set; }
    public DateTime? DemoExpirationDate { get; set; }
    public string InactiveReason { get; set; }
    public DateTime LastValidationTimestamp { get; set; }
}

public enum AccountState
{
    Active = 1,
    PaymentDue = 2,
    PaymentDueUnknown = 3,
    Disabled = 4,
    Demo = 5,
    DemoExpired = 6
}
```

### AccountStatusAudit
```csharp
using Core.Entities;

public class AccountStatusAudit : BaseEntity
{
    public Guid AccountStatusId { get; set; }
    public AccountState PreviousStatus { get; set; }
    public AccountState NewStatus { get; set; }
    public string Reason { get; set; }
    
    public virtual AccountStatus AccountStatus { get; set; }
}
```

### LicenseCache
```csharp
using Core.Entities;

public class LicenseCache : BaseEntity
{
    public string EncryptedData { get; set; } // JSON encrypted license data
    public DateTime CacheTimestamp { get; set; }
    public DateTime ExpirationTimestamp { get; set; }
    public string DataHash { get; set; } // For integrity verification
}
```

## Phase 3 Entities

### CloudSyncQueue
```csharp
using Core.Entities;

public class CloudSyncQueue : BaseEntity
{
    public string EventType { get; set; } // device_register, device_activate, etc.
    public string DeviceUuid { get; set; }
    public string RequestPayload { get; set; } // JSON serialized request
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime NextRetryAt { get; set; }
    public CloudSyncStatus Status { get; set; }
    public string LastError { get; set; }
}

public enum CloudSyncStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Abandoned = 5
}
```

## DbContext Configuration

```csharp
public class LicenseDbContext : DbContext
{
    public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options) { }

    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceAudit> DeviceAudits { get; set; }
    public DbSet<AccountStatus> AccountStatuses { get; set; }
    public DbSet<AccountStatusAudit> AccountStatusAudits { get; set; }
    public DbSet<LicenseCache> LicenseCaches { get; set; }
    public DbSet<CloudSyncQueue> CloudSyncQueue { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Device configuration
        modelBuilder.Entity<Device>(entity =>
        {
            entity.Property(e => e.DeviceUuid)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.DeviceName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.StatusNotes)
                .HasMaxLength(500);
            entity.HasIndex(e => e.DeviceUuid)
                .IsUnique();
        });

        // Device Audit configuration
        modelBuilder.Entity<DeviceAudit>(entity =>
        {
            entity.Property(e => e.Reason)
                .HasMaxLength(500);
            entity.HasOne(e => e.Device)
                .WithMany()
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AccountStatus configuration
        modelBuilder.Entity<AccountStatus>(entity =>
        {
            entity.Property(e => e.InactiveReason).HasMaxLength(500);
        });

        // AccountStatusAudit configuration
        modelBuilder.Entity<AccountStatusAudit>(entity =>
        {
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.HasOne(e => e.AccountStatus)
                .WithMany()
                .HasForeignKey(e => e.AccountStatusId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LicenseCache configuration
        modelBuilder.Entity<LicenseCache>(entity =>
        {
            entity.Property(e => e.EncryptedData).IsRequired();
            entity.Property(e => e.DataHash).HasMaxLength(256);
        });

        // CloudSyncQueue configuration
        modelBuilder.Entity<CloudSyncQueue>(entity =>
        {
            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.DeviceUuid)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.RequestPayload).IsRequired();
            entity.Property(e => e.LastError).HasMaxLength(1000);
            entity.HasIndex(e => new { e.Status, e.NextRetryAt });
            entity.HasIndex(e => e.DeviceUuid);
        });
    }
}
```

## Notes

1. All entities inherit from `BaseEntity` which provides:
   - `Id` (Guid)
   - `CreatedAt`, `CreatedByUserId`, `CreatedByUser`
   - `UpdatedAt`, `UpdatedByUserId`, `UpdatedByUser`
   - `Deleted`, `DeletedAt` (for soft deletes)

2. System actions (background services, automated processes) will have null `CreatedByUserId` and `UpdatedByUserId`

3. User actions will populate these fields from `SessionInfo.Guid`

4. All foreign keys use Guid to match the BaseEntity ID type

5. The SessionInfo class should be extended to include `DeviceUuid` property for device tracking