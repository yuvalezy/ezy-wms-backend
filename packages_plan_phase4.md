# Phase 4: Validation & Consistency Management

## 4.1 SAP Consistency Validation Service

### 4.1.1 Interface Definition

```csharp
public interface IPackageConsistencyService
{
    Task<IEnumerable<PackageInconsistency>> DetectInconsistenciesAsync(string whsCode = null);
    Task<PackageValidationResult> ValidatePackageAsync(Guid packageId);
    Task<bool> LockInconsistentPackagesAsync();
    Task RunScheduledValidationAsync();
    Task<ConsistencyReport> GenerateConsistencyReportAsync(string whsCode = null);
    Task<bool> ResolveInconsistencyAsync(Guid inconsistencyId, string resolutionAction, string userId);
}
```

### 4.1.2 Service Implementation

```csharp
public class PackageConsistencyService : IPackageConsistencyService
{
    private readonly ILWDbContext _context;
    private readonly ISboItemRepository _sboRepository;
    private readonly IPackageService _packageService;
    private readonly ILogger<PackageConsistencyService> _logger;
    private readonly IConfiguration _configuration;
    
    public PackageConsistencyService(
        ILWDbContext context,
        ISboItemRepository sboRepository,
        IPackageService packageService,
        ILogger<PackageConsistencyService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _sboRepository = sboRepository;
        _packageService = packageService;
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task<IEnumerable<PackageInconsistency>> DetectInconsistenciesAsync(string whsCode = null)
    {
        var inconsistencies = new List<PackageInconsistency>();
        
        // Get all active packages (optionally filtered by warehouse)
        var packagesQuery = _context.Packages
            .Where(p => p.Status == PackageStatus.Active)
            .Include(p => p.Contents);
            
        if (!string.IsNullOrEmpty(whsCode))
        {
            packagesQuery = packagesQuery.Where(p => p.WhsCode == whsCode);
        }
        
        var packages = await packagesQuery.ToListAsync();
        
        _logger.LogInformation("Starting consistency validation for {PackageCount} packages", packages.Count);
        
        foreach (var package in packages)
        {
            try
            {
                var validation = await ValidatePackageAsync(package.Id);
                if (!validation.IsConsistent)
                {
                    inconsistencies.AddRange(validation.Inconsistencies);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating package {PackageBarcode}", package.Barcode);
                
                // Create inconsistency record for validation error
                inconsistencies.Add(new PackageInconsistency
                {
                    PackageId = package.Id,
                    PackageBarcode = package.Barcode,
                    InconsistencyType = InconsistencyType.ValidationError,
                    DetectedAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                });
            }
        }
        
        // Store inconsistencies in database for tracking
        await StoreInconsistenciesAsync(inconsistencies);
        
        _logger.LogInformation("Consistency validation completed. Found {InconsistencyCount} inconsistencies", 
            inconsistencies.Count);
        
        return inconsistencies;
    }
    
    public async Task<PackageValidationResult> ValidatePackageAsync(Guid packageId)
    {
        var package = await _context.Packages
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == packageId);
            
        if (package == null)
        {
            return new PackageValidationResult 
            { 
                PackageId = packageId,
                IsConsistent = false,
                ErrorMessage = "Package not found"
            };
        }
        
        var inconsistencies = new List<PackageInconsistency>();
        
        // Group package contents by item and batch
        var packageItems = package.Contents
            .GroupBy(c => new { c.ItemCode, c.BatchNo, c.SerialNo })
            .Select(g => new
            {
                ItemCode = g.Key.ItemCode,
                BatchNo = g.Key.BatchNo,
                SerialNo = g.Key.SerialNo,
                WmsQuantity = g.Sum(c => c.Quantity)
            });
        
        foreach (var item in packageItems)
        {
            try
            {
                // Get SAP stock for this item in this location
                var sapStock = await GetSapStockAsync(item.ItemCode, package.WhsCode, package.BinCode, item.BatchNo);
                
                // Calculate total WMS stock (loose + all packages) for this item in this location
                var totalWmsStock = await GetTotalWmsStockAsync(
                    item.ItemCode, package.WhsCode, package.BinEntry, item.BatchNo);
                
                // Check various inconsistency scenarios
                await ValidateItemConsistencyAsync(package, item, sapStock, totalWmsStock, inconsistencies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating item {ItemCode} in package {PackageBarcode}", 
                    item.ItemCode, package.Barcode);
                
                inconsistencies.Add(new PackageInconsistency
                {
                    PackageId = packageId,
                    PackageBarcode = package.Barcode,
                    ItemCode = item.ItemCode,
                    BatchNo = item.BatchNo,
                    WhsCode = package.WhsCode,
                    BinCode = package.BinCode,
                    InconsistencyType = InconsistencyType.ValidationError,
                    DetectedAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                });
            }
        }
        
        return new PackageValidationResult
        {
            PackageId = packageId,
            IsConsistent = !inconsistencies.Any(),
            Inconsistencies = inconsistencies,
            ValidatedAt = DateTime.UtcNow
        };
    }
    
    private async Task ValidateItemConsistencyAsync(
        Package package, 
        dynamic item, 
        decimal sapStock, 
        decimal totalWmsStock, 
        List<PackageInconsistency> inconsistencies)
    {
        // Scenario 1: SAP stock is less than WMS stock (overstated in WMS)
        if (sapStock < totalWmsStock)
        {
            inconsistencies.Add(new PackageInconsistency
            {
                PackageId = package.Id,
                PackageBarcode = package.Barcode,
                ItemCode = item.ItemCode,
                BatchNo = item.BatchNo,
                WhsCode = package.WhsCode,
                BinCode = package.BinCode,
                SapQuantity = sapStock,
                WmsQuantity = totalWmsStock,
                PackageQuantity = item.WmsQuantity,
                InconsistencyType = InconsistencyType.SapStockLessThanWms,
                DetectedAt = DateTime.UtcNow,
                Severity = InconsistencySeverity.High
            });
        }
        
        // Scenario 2: Package content exceeds available SAP stock
        if (item.WmsQuantity > sapStock)
        {
            inconsistencies.Add(new PackageInconsistency
            {
                PackageId = package.Id,
                PackageBarcode = package.Barcode,
                ItemCode = item.ItemCode,
                BatchNo = item.BatchNo,
                WhsCode = package.WhsCode,
                BinCode = package.BinCode,
                SapQuantity = sapStock,
                WmsQuantity = totalWmsStock,
                PackageQuantity = item.WmsQuantity,
                InconsistencyType = InconsistencyType.PackageExceedsSapStock,
                DetectedAt = DateTime.UtcNow,
                Severity = InconsistencySeverity.Critical
            });
        }
        
        // Scenario 3: Negative package quantities
        if (item.WmsQuantity < 0)
        {
            inconsistencies.Add(new PackageInconsistency
            {
                PackageId = package.Id,
                PackageBarcode = package.Barcode,
                ItemCode = item.ItemCode,
                BatchNo = item.BatchNo,
                WhsCode = package.WhsCode,
                BinCode = package.BinCode,
                PackageQuantity = item.WmsQuantity,
                InconsistencyType = InconsistencyType.NegativePackageQuantity,
                DetectedAt = DateTime.UtcNow,
                Severity = InconsistencySeverity.Critical
            });
        }
    }
    
    public async Task<bool> LockInconsistentPackagesAsync()
    {
        var inconsistencies = await DetectInconsistenciesAsync();
        var packagesToLock = inconsistencies
            .Where(i => i.Severity >= InconsistencySeverity.High)
            .Select(i => i.PackageId)
            .Distinct();
        
        var lockedCount = 0;
        foreach (var packageId in packagesToLock)
        {
            try
            {
                var inconsistencyTypes = inconsistencies
                    .Where(i => i.PackageId == packageId)
                    .Select(i => i.InconsistencyType.ToString())
                    .Distinct();
                
                var reason = $"Automatic lock due to consistency validation failure: {string.Join(", ", inconsistencyTypes)}";
                
                await _packageService.LockPackageAsync(packageId, reason);
                lockedCount++;
                
                _logger.LogWarning("Package {PackageId} locked due to inconsistencies: {Reason}", 
                    packageId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to lock package {PackageId}", packageId);
            }
        }
        
        _logger.LogInformation("Locked {LockedCount} packages due to inconsistencies", lockedCount);
        return lockedCount > 0;
    }
    
    public async Task RunScheduledValidationAsync()
    {
        _logger.LogInformation("Starting scheduled package consistency validation");
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var inconsistencies = await DetectInconsistenciesAsync();
            
            if (IsAutoLockEnabled())
            {
                await LockInconsistentPackagesAsync();
            }
            
            // Generate summary report
            var report = await GenerateConsistencyReportAsync();
            
            _logger.LogInformation(
                "Scheduled validation completed in {ElapsedMs}ms. " +
                "Packages validated: {ValidatedCount}, Inconsistencies: {InconsistencyCount}, " +
                "Locked packages: {LockedCount}",
                stopwatch.ElapsedMilliseconds,
                report.TotalPackagesValidated,
                report.TotalInconsistencies,
                report.LockedPackages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled package validation");
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }
    
    private async Task<decimal> GetSapStockAsync(string itemCode, string whsCode, string binCode, string batchNo)
    {
        // Use existing SAP integration to get current stock
        return await _sboRepository.GetItemStockAsync(itemCode, whsCode, binCode, batchNo);
    }
    
    private async Task<decimal> GetTotalWmsStockAsync(string itemCode, string whsCode, int? binEntry, string batchNo)
    {
        // Get package stock
        var packageStock = await _context.PackageContents
            .Where(pc => pc.ItemCode == itemCode && 
                        pc.WhsCode == whsCode && 
                        pc.BinEntry == binEntry &&
                        (batchNo == null || pc.BatchNo == batchNo))
            .SumAsync(pc => pc.Quantity);
        
        // Get loose stock (implementation depends on existing stock tracking)
        var looseStock = await GetLooseStockAsync(itemCode, whsCode, binEntry, batchNo);
        
        return packageStock + looseStock;
    }
    
    private async Task<decimal> GetLooseStockAsync(string itemCode, string whsCode, int? binEntry, string batchNo)
    {
        // Implementation depends on existing loose stock tracking
        // This might query bin content tables or other stock tracking systems
        // For now, return 0 as packages are the primary concern
        return 0;
    }
    
    private async Task StoreInconsistenciesAsync(IEnumerable<PackageInconsistency> inconsistencies)
    {
        // Remove old inconsistencies for the same packages
        var packageIds = inconsistencies.Select(i => i.PackageId).Distinct();
        var existingInconsistencies = await _context.PackageInconsistencies
            .Where(i => packageIds.Contains(i.PackageId) && !i.IsResolved)
            .ToListAsync();
        
        _context.PackageInconsistencies.RemoveRange(existingInconsistencies);
        
        // Add new inconsistencies
        foreach (var inconsistency in inconsistencies)
        {
            inconsistency.Id = Guid.NewGuid();
            _context.PackageInconsistencies.Add(inconsistency);
        }
        
        await _context.SaveChangesAsync();
    }
    
    private bool IsAutoLockEnabled()
    {
        return _configuration.GetValue<bool>("Package:Validation:AutoLockInconsistentPackages", true);
    }
}
```

## 4.2 Scheduled Validation Background Service

```csharp
public class PackageValidationHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PackageValidationHostedService> _logger;
    
    public PackageValidationHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<PackageValidationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue<int>("Package:Validation:IntervalMinutes", 30);
        var isEnabled = _configuration.GetValue<bool>("Package:Validation:EnableScheduledValidation", true);
        
        if (!isEnabled)
        {
            _logger.LogInformation("Scheduled package validation is disabled");
            return;
        }
        
        var interval = TimeSpan.FromMinutes(intervalMinutes);
        
        _logger.LogInformation("Package validation service started. Interval: {IntervalMinutes} minutes", intervalMinutes);
        
        // Wait for initial delay to avoid startup conflicts
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var consistencyService = scope.ServiceProvider.GetRequiredService<IPackageConsistencyService>();
                
                _logger.LogInformation("Starting scheduled package consistency validation");
                
                await consistencyService.RunScheduledValidationAsync();
                
                _logger.LogInformation("Completed scheduled package consistency validation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled package validation");
            }
            
            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
        }
        
        _logger.LogInformation("Package validation service stopped");
    }
}
```

## 4.3 Real-time Validation Integration

### 4.3.1 Real-time Validation Service

```csharp
public class RealTimePackageValidationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RealTimePackageValidationService> _logger;
    private Timer _validationTimer;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var isEnabled = _configuration.GetValue<bool>("Package:Validation:EnableRealTimeValidation", true);
        if (!isEnabled) return;
        
        _logger.LogInformation("Real-time package validation service started");
        
        // Set up timer for periodic real-time checks (every 5 minutes)
        _validationTimer = new Timer(ValidateRecentChanges, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _validationTimer?.Change(Timeout.Infinite, 0);
        _logger.LogInformation("Real-time package validation service stopped");
        return Task.CompletedTask;
    }
    
    private async void ValidateRecentChanges(object state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ILWDbContext>();
            var consistencyService = scope.ServiceProvider.GetRequiredService<IPackageConsistencyService>();
            
            // Get packages modified in the last 10 minutes
            var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
            var recentlyModifiedPackages = await context.Packages
                .Where(p => p.Status == PackageStatus.Active && p.UpdatedAt >= cutoffTime)
                .Select(p => p.Id)
                .ToListAsync();
            
            if (recentlyModifiedPackages.Any())
            {
                _logger.LogDebug("Validating {Count} recently modified packages", recentlyModifiedPackages.Count);
                
                foreach (var packageId in recentlyModifiedPackages)
                {
                    var validation = await consistencyService.ValidatePackageAsync(packageId);
                    if (!validation.IsConsistent)
                    {
                        _logger.LogWarning("Real-time validation found inconsistencies in package {PackageId}", packageId);
                        
                        // Auto-lock if configured
                        if (_configuration.GetValue<bool>("Package:Validation:AutoLockInconsistentPackages", true))
                        {
                            // Lock only critical inconsistencies in real-time
                            var criticalInconsistencies = validation.Inconsistencies
                                .Where(i => i.Severity == InconsistencySeverity.Critical);
                            
                            if (criticalInconsistencies.Any())
                            {
                                var packageService = scope.ServiceProvider.GetRequiredService<IPackageService>();
                                var reason = $"Real-time validation failure: {string.Join(", ", criticalInconsistencies.Select(i => i.InconsistencyType))}";
                                await packageService.LockPackageAsync(packageId, reason);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during real-time package validation");
        }
    }
}
```

### 4.3.2 Pre-Operation Validation

```csharp
public class PackageOperationValidator
{
    private readonly IPackageConsistencyService _consistencyService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PackageOperationValidator> _logger;
    
    public PackageOperationValidator(
        IPackageConsistencyService consistencyService,
        IConfiguration configuration,
        ILogger<PackageOperationValidator> logger)
    {
        _consistencyService = consistencyService;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<OperationValidationResult> ValidateBeforeOperationAsync(Guid packageId, string operationType)
    {
        if (!IsRealTimeValidationEnabled())
        {
            return OperationValidationResult.Success();
        }
        
        try
        {
            var validation = await _consistencyService.ValidatePackageAsync(packageId);
            
            if (!validation.IsConsistent)
            {
                var criticalIssues = validation.Inconsistencies
                    .Where(i => i.Severity >= InconsistencySeverity.High)
                    .ToList();
                
                if (criticalIssues.Any())
                {
                    var errorMessage = $"Package has critical consistency issues: {string.Join(", ", criticalIssues.Select(i => i.InconsistencyType))}";
                    
                    _logger.LogWarning("Operation {OperationType} blocked for package {PackageId}: {Reason}", 
                        operationType, packageId, errorMessage);
                    
                    return OperationValidationResult.Failure(errorMessage);
                }
            }
            
            return OperationValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating package {PackageId} before operation {OperationType}", 
                packageId, operationType);
            
            // In case of validation errors, allow operation to proceed but log the issue
            return OperationValidationResult.Success();
        }
    }
    
    private bool IsRealTimeValidationEnabled()
    {
        return _configuration.GetValue<bool>("Package:Validation:EnableRealTimeValidation", true);
    }
}

public class OperationValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
    
    public static OperationValidationResult Success() => new() { IsValid = true };
    public static OperationValidationResult Failure(string message) => new() { IsValid = false, ErrorMessage = message };
}
```

## 4.4 Inconsistency Management

### 4.4.1 Inconsistency Entity

```csharp
public class PackageInconsistency : BaseEntity
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public string PackageBarcode { get; set; }
    public string ItemCode { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
    public string WhsCode { get; set; }
    public string BinCode { get; set; }
    public decimal? SapQuantity { get; set; }
    public decimal? WmsQuantity { get; set; }
    public decimal? PackageQuantity { get; set; }
    public InconsistencyType InconsistencyType { get; set; }
    public InconsistencySeverity Severity { get; set; }
    public DateTime DetectedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string ResolvedBy { get; set; }
    public string ResolutionAction { get; set; }
    public string ErrorMessage { get; set; }
    public string Notes { get; set; }
    
    // Navigation properties
    public virtual Package Package { get; set; }
}

public enum InconsistencyType
{
    SapStockLessThanWms,
    PackageExceedsSapStock,
    NegativePackageQuantity,
    ValidationError,
    LocationMismatch,
    DuplicateContent
}

public enum InconsistencySeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
```

### 4.4.2 Inconsistency Controller

```csharp
[ApiController]
[Route("api/package/inconsistencies")]
[Authorize]
public class PackageInconsistencyController : ControllerBase
{
    private readonly IPackageConsistencyService _consistencyService;
    private readonly ILWDbContext _context;
    private readonly ILogger<PackageInconsistencyController> _logger;
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PackageInconsistencyDto>>> GetInconsistencies(
        [FromQuery] string whsCode = null,
        [FromQuery] bool includeResolved = false)
    {
        var query = _context.PackageInconsistencies.AsQueryable();
        
        if (!string.IsNullOrEmpty(whsCode))
        {
            query = query.Where(i => i.WhsCode == whsCode);
        }
        
        if (!includeResolved)
        {
            query = query.Where(i => !i.IsResolved);
        }
        
        var inconsistencies = await query
            .OrderByDescending(i => i.DetectedAt)
            .ToListAsync();
        
        return Ok(inconsistencies.Select(i => i.ToDto()));
    }
    
    [HttpPost("validate")]
    public async Task<ActionResult<IEnumerable<PackageInconsistencyDto>>> RunValidation([FromQuery] string whsCode = null)
    {
        try
        {
            var inconsistencies = await _consistencyService.DetectInconsistenciesAsync(whsCode);
            return Ok(inconsistencies.Select(i => i.ToDto()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running package validation");
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("{id}/resolve")]
    public async Task<ActionResult> ResolveInconsistency(Guid id, [FromBody] ResolveInconsistencyRequest request)
    {
        try
        {
            var success = await _consistencyService.ResolveInconsistencyAsync(id, request.ResolutionAction, EmployeeID);
            if (success)
            {
                return Ok();
            }
            else
            {
                return BadRequest(new { error = "Failed to resolve inconsistency" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving inconsistency {InconsistencyId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("report")]
    public async Task<ActionResult<ConsistencyReportDto>> GetConsistencyReport([FromQuery] string whsCode = null)
    {
        try
        {
            var report = await _consistencyService.GenerateConsistencyReportAsync(whsCode);
            return Ok(report.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating consistency report");
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

## 4.5 Models and DTOs

```csharp
public class PackageValidationResult
{
    public Guid PackageId { get; set; }
    public bool IsConsistent { get; set; }
    public List<PackageInconsistency> Inconsistencies { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
    public string ErrorMessage { get; set; }
}

public class ConsistencyReport
{
    public DateTime GeneratedAt { get; set; }
    public string WhsCode { get; set; }
    public int TotalPackagesValidated { get; set; }
    public int TotalInconsistencies { get; set; }
    public int LockedPackages { get; set; }
    public Dictionary<InconsistencyType, int> InconsistenciesByType { get; set; } = new();
    public Dictionary<InconsistencySeverity, int> InconsistenciesBySeverity { get; set; } = new();
    public List<string> MostAffectedWarehouses { get; set; } = new();
    public List<string> MostAffectedItems { get; set; } = new();
}

public class PackageInconsistencyDto
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public string PackageBarcode { get; set; }
    public string ItemCode { get; set; }
    public string BatchNo { get; set; }
    public string WhsCode { get; set; }
    public string BinCode { get; set; }
    public decimal? SapQuantity { get; set; }
    public decimal? WmsQuantity { get; set; }
    public decimal? PackageQuantity { get; set; }
    public string InconsistencyType { get; set; }
    public string Severity { get; set; }
    public DateTime DetectedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string ResolvedBy { get; set; }
    public string ResolutionAction { get; set; }
    public string ErrorMessage { get; set; }
}

public class ResolveInconsistencyRequest
{
    public string ResolutionAction { get; set; }
    public string Notes { get; set; }
}
```

## Implementation Notes

### Timeline: Week 8-9
- SAP consistency validation service with comprehensive checks
- Background validation service with configurable scheduling
- Real-time validation for critical operations
- Inconsistency management and resolution workflow

### Key Features
- **Hybrid validation approach**: Real-time + scheduled validation
- **Severity-based actions**: Critical issues auto-lock packages
- **Comprehensive validation**: Multiple inconsistency types and scenarios
- **Audit trail**: Full tracking of inconsistencies and resolutions
- **Configurable behavior**: Enable/disable features via configuration
- **Performance optimized**: Efficient validation queries and batch processing

### Validation Scenarios
1. SAP stock less than WMS stock (overstated inventory)
2. Package content exceeds available SAP stock
3. Negative package quantities
4. Validation errors and system issues
5. Location mismatches between package and contents

### Next Steps
- Phase 5: Reports and label generation system
- Integration with alerting/notification systems
- Advanced resolution workflows and automation