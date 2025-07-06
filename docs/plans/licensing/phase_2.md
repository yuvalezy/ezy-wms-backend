# Phase 2: Account Status & License Cache

## Objectives
- Implement account status management system
- Create license data caching with encryption
- Implement account status transition logic
- Add license validation functionality

## Technical Tasks

### 1. Account Status Entity

#### AccountStatus Entity
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

#### Account Status Audit Entity
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

### 2. License Cache Entity

#### LicenseCache Entity
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

#### License Cache Data Models
```csharp
public class LicenseCacheData
{
    public AccountState AccountStatus { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? PaymentCycleDate { get; set; }
    public DateTime? DemoExpirationDate { get; set; }
    public string InactiveReason { get; set; }
    public DateTime LastValidationTimestamp { get; set; }
    public int ActiveDeviceCount { get; set; }
    public int MaxAllowedDevices { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}
```

### 3. License Encryption Service

#### ILicenseEncryptionService Interface
```csharp
public interface ILicenseEncryptionService
{
    string EncryptLicenseData(LicenseCacheData data);
    LicenseCacheData DecryptLicenseData(string encryptedData);
    string GenerateDataHash(LicenseCacheData data);
    bool ValidateDataHash(LicenseCacheData data, string hash);
}
```

#### LicenseEncryptionService Implementation
```csharp
public class LicenseEncryptionService : ILicenseEncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LicenseEncryptionService> _logger;
    private readonly string _encryptionKey;

    public LicenseEncryptionService(IConfiguration configuration, ILogger<LicenseEncryptionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _encryptionKey = configuration["Licensing:EncryptionKey"] ?? 
            throw new InvalidOperationException("Licensing encryption key not configured");
    }

    public string EncryptLicenseData(LicenseCacheData data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var plainTextBytes = Encoding.UTF8.GetBytes(json);
            
            using (var aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(_encryptionKey);
                aes.GenerateIV();
                
                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plainTextBytes, 0, plainTextBytes.Length);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt license data");
            throw;
        }
    }

    public LicenseCacheData DecryptLicenseData(string encryptedData)
    {
        try
        {
            var fullCipher = Convert.FromBase64String(encryptedData);
            
            using (var aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(_encryptionKey);
                
                var iv = new byte[16];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    var json = srDecrypt.ReadToEnd();
                    return JsonUtils.Deserialize<LicenseCacheData>(json);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt license data");
            throw;
        }
    }

    public string GenerateDataHash(LicenseCacheData data)
    {
        var json = JsonSerializer.Serialize(data);
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hash);
        }
    }

    public bool ValidateDataHash(LicenseCacheData data, string hash)
    {
        var computedHash = GenerateDataHash(data);
        return computedHash == hash;
    }
}
```

### 4. Account Status Service

#### IAccountStatusService Interface
```csharp
public interface IAccountStatusService
{
    Task<AccountStatus> GetCurrentAccountStatusAsync();
    Task<AccountStatus> UpdateAccountStatusAsync(AccountState newStatus, string reason);
    Task<bool> IsAccountActiveAsync();
    Task<bool> IsPaymentDueAsync();
    Task<bool> IsSystemAccessAllowedAsync();
    Task<List<AccountStatusAudit>> GetStatusHistoryAsync();
    Task ProcessAccountStatusTransitionAsync();
}
```

#### AccountStatusService Implementation
```csharp
public class AccountStatusService : IAccountStatusService
{
    private readonly LicenseDbContext _context;
    private readonly ILogger<AccountStatusService> _logger;

    public AccountStatusService(LicenseDbContext context, ILogger<AccountStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AccountStatus> GetCurrentAccountStatusAsync()
    {
        return await _context.AccountStatuses
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync() ?? CreateInitialAccountStatus();
    }

    public async Task<AccountStatus> UpdateAccountStatusAsync(AccountState newStatus, string reason)
    {
        var current = await GetCurrentAccountStatusAsync();
        
        if (current.Status == newStatus)
        {
            _logger.LogInformation("Account status already {Status}", newStatus);
            return current;
        }

        var previousStatus = current.Status;
        current.Status = newStatus;
        current.UpdatedAt = DateTime.UtcNow;
        // UpdatedByUserId will be null for system updates
        current.LastValidationTimestamp = DateTime.UtcNow;

        // Update specific fields based on new status
        switch (newStatus)
        {
            case AccountState.PaymentDue:
                if (current.PaymentCycleDate.HasValue && current.PaymentCycleDate < DateTime.UtcNow)
                {
                    current.ExpirationDate = DateTime.UtcNow.AddDays(30); // Grace period
                }
                break;
            case AccountState.PaymentDueUnknown:
                current.ExpirationDate = DateTime.UtcNow.AddDays(7); // 7-day grace period
                break;
            case AccountState.Disabled:
                current.InactiveReason = reason ?? "Account disabled due to payment issues";
                break;
            case AccountState.DemoExpired:
                current.InactiveReason = reason ?? "Demo period expired";
                break;
        }

        await _context.SaveChangesAsync();

        // Log audit record
        await LogStatusChangeAsync(current.Id, previousStatus, newStatus, reason);

        _logger.LogInformation("Account status changed from {PreviousStatus} to {NewStatus}: {Reason}", 
            previousStatus, newStatus, reason);

        return current;
    }

    public async Task<bool> IsAccountActiveAsync()
    {
        var status = await GetCurrentAccountStatusAsync();
        return status.Status == AccountState.Active;
    }

    public async Task<bool> IsPaymentDueAsync()
    {
        var status = await GetCurrentAccountStatusAsync();
        return status.Status == AccountState.PaymentDue || 
               status.Status == AccountState.PaymentDueUnknown;
    }

    public async Task<bool> IsSystemAccessAllowedAsync()
    {
        var status = await GetCurrentAccountStatusAsync();
        return status.Status == AccountState.Active || 
               status.Status == AccountState.PaymentDue ||
               status.Status == AccountState.PaymentDueUnknown ||
               status.Status == AccountState.Demo;
    }

    public async Task ProcessAccountStatusTransitionAsync()
    {
        var status = await GetCurrentAccountStatusAsync();
        var now = DateTime.UtcNow;

        switch (status.Status)
        {
            case AccountState.Active:
                if (status.PaymentCycleDate.HasValue && status.PaymentCycleDate < now)
                {
                    await UpdateAccountStatusAsync(AccountState.PaymentDue, "Payment cycle date reached");
                }
                break;

            case AccountState.PaymentDueUnknown:
                if (status.ExpirationDate.HasValue && status.ExpirationDate < now)
                {
                    await UpdateAccountStatusAsync(AccountState.Disabled, "Grace period expired");
                }
                break;

            case AccountState.Demo:
                if (status.DemoExpirationDate.HasValue && status.DemoExpirationDate < now)
                {
                    await UpdateAccountStatusAsync(AccountState.DemoExpired, "Demo period expired");
                }
                break;
        }
    }

    private AccountStatus CreateInitialAccountStatus()
    {
        return new AccountStatus
        {
            Status = AccountState.Demo,
            DemoExpirationDate = DateTime.UtcNow.AddDays(30),
            LastValidationTimestamp = DateTime.UtcNow
        };
    }

    private async Task LogStatusChangeAsync(Guid accountStatusId, AccountState previousStatus, 
        AccountState newStatus, string reason)
    {
        var audit = new AccountStatusAudit
        {
            AccountStatusId = accountStatusId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Reason = reason
            // CreatedByUserId will be null for system actions
        };

        _context.AccountStatusAudits.Add(audit);
        await _context.SaveChangesAsync();
    }
}
```

### 5. License Cache Service

#### ILicenseCacheService Interface
```csharp
public interface ILicenseCacheService
{
    Task<LicenseCacheData> GetLicenseCacheAsync();
    Task UpdateLicenseCacheAsync(LicenseCacheData data);
    Task<bool> IsLicenseCacheValidAsync();
    Task InvalidateLicenseCacheAsync();
    Task<DateTime?> GetLastValidationTimestampAsync();
}
```

#### LicenseCacheService Implementation
```csharp
public class LicenseCacheService : ILicenseCacheService
{
    private readonly LicenseDbContext _context;
    private readonly ILicenseEncryptionService _encryptionService;
    private readonly IAccountStatusService _accountStatusService;
    private readonly ILogger<LicenseCacheService> _logger;

    public LicenseCacheService(
        LicenseDbContext context,
        ILicenseEncryptionService encryptionService,
        IAccountStatusService accountStatusService,
        ILogger<LicenseCacheService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _accountStatusService = accountStatusService;
        _logger = logger;
    }

    public async Task<LicenseCacheData> GetLicenseCacheAsync()
    {
        var cache = await _context.LicenseCaches
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();

        if (cache == null || cache.ExpirationTimestamp < DateTime.UtcNow)
        {
            _logger.LogWarning("License cache not found or expired");
            return await CreateDefaultLicenseCacheAsync();
        }

        try
        {
            var data = _encryptionService.DecryptLicenseData(cache.EncryptedData);
            
            // Validate data integrity
            if (!_encryptionService.ValidateDataHash(data, cache.DataHash))
            {
                _logger.LogError("License cache data integrity check failed");
                return await CreateDefaultLicenseCacheAsync();
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt license cache");
            return await CreateDefaultLicenseCacheAsync();
        }
    }

    public async Task UpdateLicenseCacheAsync(LicenseCacheData data)
    {
        try
        {
            var encryptedData = _encryptionService.EncryptLicenseData(data);
            var dataHash = _encryptionService.GenerateDataHash(data);

            var cache = new LicenseCache
            {
                EncryptedData = encryptedData,
                DataHash = dataHash,
                CacheTimestamp = DateTime.UtcNow,
                ExpirationTimestamp = DateTime.UtcNow.AddHours(24) // Cache for 24 hours
            };

            _context.LicenseCaches.Add(cache);
            await _context.SaveChangesAsync();

            _logger.LogInformation("License cache updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update license cache");
            throw;
        }
    }

    public async Task<bool> IsLicenseCacheValidAsync()
    {
        var cache = await _context.LicenseCaches
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();

        return cache != null && cache.ExpirationTimestamp > DateTime.UtcNow;
    }

    public async Task InvalidateLicenseCacheAsync()
    {
        var cache = await _context.LicenseCaches
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();

        if (cache != null)
        {
            cache.ExpirationTimestamp = DateTime.UtcNow.AddMinutes(-1); // Expire immediately
            await _context.SaveChangesAsync();
            _logger.LogInformation("License cache invalidated");
        }
    }

    public async Task<DateTime?> GetLastValidationTimestampAsync()
    {
        var cache = await GetLicenseCacheAsync();
        return cache?.LastValidationTimestamp;
    }

    private async Task<LicenseCacheData> CreateDefaultLicenseCacheAsync()
    {
        var accountStatus = await _accountStatusService.GetCurrentAccountStatusAsync();
        
        return new LicenseCacheData
        {
            AccountStatus = accountStatus.Status,
            ExpirationDate = accountStatus.ExpirationDate,
            PaymentCycleDate = accountStatus.PaymentCycleDate,
            DemoExpirationDate = accountStatus.DemoExpirationDate,
            InactiveReason = accountStatus.InactiveReason,
            LastValidationTimestamp = DateTime.UtcNow,
            ActiveDeviceCount = 0,
            MaxAllowedDevices = 1, // Default
            AdditionalData = new Dictionary<string, object>()
        };
    }
}
```

### 6. License Validation Service

#### ILicenseValidationService Interface
```csharp
public interface ILicenseValidationService
{
    Task<bool> ValidateDeviceAccessAsync(string deviceUuid);
    Task<bool> ValidateSystemAccessAsync();
    Task<LicenseValidationResult> GetLicenseValidationResultAsync();
    Task<bool> IsWithinGracePeriodAsync();
    Task<int> GetDaysUntilExpirationAsync();
}
```

#### LicenseValidationResult
```csharp
public class LicenseValidationResult
{
    public bool IsValid { get; set; }
    public bool IsInGracePeriod { get; set; }
    public bool ShowWarning { get; set; }
    public string WarningMessage { get; set; }
    public AccountState AccountStatus { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int DaysUntilExpiration { get; set; }
}
```

#### LicenseValidationService Implementation
```csharp
public class LicenseValidationService : ILicenseValidationService
{
    private readonly IAccountStatusService _accountStatusService;
    private readonly IDeviceService _deviceService;
    private readonly ILicenseCacheService _licenseCacheService;
    private readonly ILogger<LicenseValidationService> _logger;

    public LicenseValidationService(
        IAccountStatusService accountStatusService,
        IDeviceService deviceService,
        ILicenseCacheService licenseCacheService,
        ILogger<LicenseValidationService> logger)
    {
        _accountStatusService = accountStatusService;
        _deviceService = deviceService;
        _licenseCacheService = licenseCacheService;
        _logger = logger;
    }

    public async Task<bool> ValidateDeviceAccessAsync(string deviceUuid)
    {
        // Check if device exists and is active
        var device = await _deviceService.GetDeviceAsync(deviceUuid);
        if (device == null || device.Status != DeviceStatus.Active)
        {
            _logger.LogWarning("Device {DeviceUuid} not found or not active", deviceUuid);
            return false;
        }

        // Check account status
        var systemAccess = await ValidateSystemAccessAsync();
        if (!systemAccess)
        {
            _logger.LogWarning("System access denied for device {DeviceUuid} due to account status", deviceUuid);
            return false;
        }

        return true;
    }

    public async Task<bool> ValidateSystemAccessAsync()
    {
        var accountStatus = await _accountStatusService.GetCurrentAccountStatusAsync();
        
        switch (accountStatus.Status)
        {
            case AccountState.Active:
            case AccountState.PaymentDue:
            case AccountState.PaymentDueUnknown:
            case AccountState.Demo:
                return true;
            
            case AccountState.Disabled:
            case AccountState.DemoExpired:
                return false;
            
            default:
                _logger.LogWarning("Unknown account status: {Status}", accountStatus.Status);
                return false;
        }
    }

    public async Task<LicenseValidationResult> GetLicenseValidationResultAsync()
    {
        var accountStatus = await _accountStatusService.GetCurrentAccountStatusAsync();
        var licenseCache = await _licenseCacheService.GetLicenseCacheAsync();
        
        var result = new LicenseValidationResult
        {
            AccountStatus = accountStatus.Status,
            ExpirationDate = accountStatus.ExpirationDate ?? accountStatus.DemoExpirationDate,
            IsValid = await ValidateSystemAccessAsync()
        };

        if (result.ExpirationDate.HasValue)
        {
            var daysUntilExpiration = (int)(result.ExpirationDate.Value - DateTime.UtcNow).TotalDays;
            result.DaysUntilExpiration = Math.Max(0, daysUntilExpiration);
            result.IsInGracePeriod = daysUntilExpiration > 0 && daysUntilExpiration <= 7;
        }

        // Determine warning message
        if (accountStatus.Status == AccountState.PaymentDue)
        {
            result.ShowWarning = true;
            result.WarningMessage = "Payment is due. Please contact support.";
        }
        else if (accountStatus.Status == AccountState.PaymentDueUnknown)
        {
            result.ShowWarning = true;
            result.WarningMessage = $"Payment status unknown. System will be disabled in {result.DaysUntilExpiration} days.";
        }
        else if (result.IsInGracePeriod)
        {
            result.ShowWarning = true;
            result.WarningMessage = $"Account expires in {result.DaysUntilExpiration} days. Please renew.";
        }

        return result;
    }

    public async Task<bool> IsWithinGracePeriodAsync()
    {
        var result = await GetLicenseValidationResultAsync();
        return result.IsInGracePeriod;
    }

    public async Task<int> GetDaysUntilExpirationAsync()
    {
        var result = await GetLicenseValidationResultAsync();
        return result.DaysUntilExpiration;
    }
}
```

### 7. Database Schema Updates

#### Updated LicenseDbContext
```csharp
public class LicenseDbContext : DbContext
{
    public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options) { }

    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceAudit> DeviceAudits { get; set; }
    public DbSet<AccountStatus> AccountStatuses { get; set; }
    public DbSet<AccountStatusAudit> AccountStatusAudits { get; set; }
    public DbSet<LicenseCache> LicenseCaches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Previous Device and DeviceAudit configurations...

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
    }
}
```

## Configuration Updates

### appsettings.json
```json
{
  "Licensing": {
    "EncryptionKey": "BASE64_ENCODED_32_BYTE_KEY",
    "CacheExpirationHours": 24,
    "GracePeriodDays": 7,
    "DemoExpirationDays": 30
  }
}
```

### Startup.cs Updates
```csharp
services.AddScoped<ILicenseEncryptionService, LicenseEncryptionService>();
services.AddScoped<IAccountStatusService, AccountStatusService>();
services.AddScoped<ILicenseCacheService, LicenseCacheService>();
services.AddScoped<ILicenseValidationService, LicenseValidationService>();
```

## Testing Approach

### Unit Tests
```csharp
[TestFixture]
public class LicenseEncryptionServiceTests
{
    [Test]
    public void EncryptDecrypt_ValidData_ReturnsOriginalData()
    {
        // Test encryption/decryption round trip
    }

    [Test]
    public void GenerateDataHash_SameData_ReturnsSameHash()
    {
        // Test hash generation consistency
    }
}

[TestFixture]
public class AccountStatusServiceTests
{
    [Test]
    public async Task UpdateAccountStatus_ValidTransition_UpdatesStatus()
    {
        // Test status transitions
    }

    [Test]
    public async Task ProcessAccountStatusTransition_PaymentDue_TransitionsCorrectly()
    {
        // Test automatic status transitions
    }
}

[TestFixture]
public class LicenseValidationServiceTests
{
    [Test]
    public async Task ValidateSystemAccess_ActiveAccount_ReturnsTrue()
    {
        // Test system access validation
    }

    [Test]
    public async Task ValidateDeviceAccess_InactiveDevice_ReturnsFalse()
    {
        // Test device access validation
    }
}
```

### Integration Tests
```csharp
[Test]
public async Task LicenseCache_FullCycle_WorksCorrectly()
{
    // Test full cache lifecycle
}

[Test]
public async Task AccountStatusTransition_FromActiveToDisabled_WorksCorrectly()
{
    // Test complete status transition flow
}
```

## Success Criteria

- [ ] Account status management system implemented
- [ ] License data encryption/decryption working
- [ ] License cache system functional
- [ ] Account status transitions automated
- [ ] License validation service operational
- [ ] All unit tests pass
- [ ] Integration tests validate full workflow
- [ ] Data integrity maintained throughout

## Dependencies

### Phase 1 Components
- Device management system
- Database context and entities
- Basic authentication framework

### External Dependencies
- Configuration system for encryption keys
- Logging infrastructure

## Deliverables

1. Account status entities and services
2. License cache with encryption
3. License validation service
4. Database migration for new entities
5. Configuration updates
6. Unit and integration tests
7. Documentation updates

## Next Phase

Phase 3 will implement cloud server integration and synchronization mechanisms to communicate with the external licensing service.