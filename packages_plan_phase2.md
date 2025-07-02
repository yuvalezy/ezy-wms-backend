# Phase 2: Package Management Services & API

## 2.1 Package Service Interface

```csharp
public interface IPackageService
{
    // Core Package Operations
    Task<Package> CreatePackageAsync(CreatePackageRequest request);
    Task<Package> GetPackageAsync(Guid packageId);
    Task<Package> GetPackageByBarcodeAsync(string barcode);
    Task<IEnumerable<Package>> GetActivePackagesAsync(string whsCode = null);
    Task<Package> ClosePackageAsync(Guid packageId, string userId);
    Task<Package> CancelPackageAsync(Guid packageId, string userId, string reason);
    Task<Package> LockPackageAsync(Guid packageId, string reason);
    Task<Package> UnlockPackageAsync(Guid packageId, string userId);
    
    // Content Management
    Task<PackageContent> AddItemToPackageAsync(AddItemToPackageRequest request);
    Task<PackageContent> RemoveItemFromPackageAsync(RemoveItemFromPackageRequest request);
    Task<IEnumerable<PackageContent>> GetPackageContentsAsync(Guid packageId);
    Task<decimal> GetItemQuantityInPackageAsync(Guid packageId, string itemCode);
    
    // Location Management
    Task<Package> MovePackageAsync(MovePackageRequest request);
    Task<IEnumerable<PackageLocationHistory>> GetPackageLocationHistoryAsync(Guid packageId);
    
    // Validation & Consistency
    Task<PackageValidationResult> ValidatePackageConsistencyAsync(Guid packageId);
    Task<IEnumerable<PackageInconsistency>> DetectInconsistenciesAsync(string whsCode = null);
    
    // Barcode Management
    Task<string> GeneratePackageBarcodeAsync();
    Task<bool> ValidatePackageBarcodeAsync(string barcode);
    
    // Transaction History
    Task<IEnumerable<PackageTransaction>> GetPackageTransactionHistoryAsync(Guid packageId);
    Task LogPackageTransactionAsync(LogPackageTransactionRequest request);
}
```

## 2.2 Package Service Implementation

```csharp
public class PackageService : IPackageService
{
    private readonly ILWDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PackageService> _logger;
    private readonly ISboItemRepository _itemRepository;
    
    public PackageService(
        ILWDbContext context,
        IConfiguration configuration,
        ILogger<PackageService> logger,
        ISboItemRepository itemRepository)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _itemRepository = itemRepository;
    }
    
    public async Task<Package> CreatePackageAsync(CreatePackageRequest request)
    {
        // Validate package creation settings
        if (!IsPackageFeatureEnabled())
        {
            throw new InvalidOperationException("Package feature is not enabled");
        }
        
        // Generate barcode
        var barcode = await GeneratePackageBarcodeAsync();
        
        var package = new Package
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            Status = PackageStatus.Init,
            WhsCode = request.WhsCode,
            BinEntry = request.BinEntry,
            BinCode = request.BinCode,
            CreatedBy = request.UserId,
            CreatedAt = DateTime.UtcNow,
            CustomAttributes = SerializeCustomAttributes(request.CustomAttributes)
        };
        
        _context.Packages.Add(package);
        
        // Log location history
        await LogLocationMovementAsync(package.Id, PackageMovementType.Created, 
            null, null, null, request.WhsCode, request.BinEntry, request.BinCode,
            "PackageCreation", null, request.UserId);
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Package created: {Barcode} by user {UserId}", barcode, request.UserId);
        
        return package;
    }
    
    public async Task<Package> GetPackageAsync(Guid packageId)
    {
        return await _context.Packages
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == packageId);
    }
    
    public async Task<Package> GetPackageByBarcodeAsync(string barcode)
    {
        return await _context.Packages
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
    }
    
    public async Task<IEnumerable<Package>> GetActivePackagesAsync(string whsCode = null)
    {
        var query = _context.Packages
            .Where(p => p.Status == PackageStatus.Active);
            
        if (!string.IsNullOrEmpty(whsCode))
        {
            query = query.Where(p => p.WhsCode == whsCode);
        }
        
        return await query.ToListAsync();
    }
    
    public async Task<Package> ClosePackageAsync(Guid packageId, string userId)
    {
        var package = await GetPackageAsync(packageId);
        if (package == null)
        {
            throw new NotFoundException($"Package {packageId} not found");
        }
        
        if (package.Status != PackageStatus.Active)
        {
            throw new InvalidOperationException($"Package {package.Barcode} is not active");
        }
        
        package.Status = PackageStatus.Closed;
        package.ClosedAt = DateTime.UtcNow;
        package.ClosedBy = userId;
        package.UpdatedAt = DateTime.UtcNow;
        package.UpdatedBy = userId;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Package closed: {Barcode} by user {UserId}", package.Barcode, userId);
        
        return package;
    }
    
    public async Task<Package> LockPackageAsync(Guid packageId, string reason)
    {
        var package = await GetPackageAsync(packageId);
        if (package == null)
        {
            throw new NotFoundException($"Package {packageId} not found");
        }
        
        package.Status = PackageStatus.Locked;
        package.Notes = reason;
        package.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogWarning("Package locked: {Barcode} - Reason: {Reason}", package.Barcode, reason);
        
        return package;
    }
    
    public async Task<PackageContent> AddItemToPackageAsync(AddItemToPackageRequest request)
    {
        var package = await GetPackageAsync(request.PackageId);
        if (package == null)
        {
            throw new NotFoundException($"Package {request.PackageId} not found");
        }
        
        if (package.Status == PackageStatus.Locked)
        {
            throw new InvalidOperationException($"Package {package.Barcode} is locked");
        }
        
        if (package.Status == PackageStatus.Closed)
        {
            throw new InvalidOperationException($"Package {package.Barcode} is closed");
        }
        
        // Validate location consistency
        if (request.WhsCode != package.WhsCode || request.BinEntry != package.BinEntry)
        {
            throw new InvalidOperationException("Item location must match package location");
        }
        
        var content = new PackageContent
        {
            Id = Guid.NewGuid(),
            PackageId = request.PackageId,
            ItemCode = request.ItemCode,
            Quantity = request.Quantity,
            UnitCode = request.UnitCode,
            BatchNo = request.BatchNo,
            SerialNo = request.SerialNo,
            ExpiryDate = request.ExpiryDate,
            WhsCode = request.WhsCode,
            BinEntry = request.BinEntry,
            BinCode = request.BinCode,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.UserId
        };
        
        _context.PackageContents.Add(content);
        
        // Log transaction
        await LogPackageTransactionAsync(new LogPackageTransactionRequest
        {
            PackageId = request.PackageId,
            TransactionType = PackageTransactionType.Add,
            ItemCode = request.ItemCode,
            Quantity = request.Quantity,
            UnitCode = request.UnitCode,
            BatchNo = request.BatchNo,
            SerialNo = request.SerialNo,
            SourceOperationType = request.SourceOperationType,
            SourceOperationId = request.SourceOperationId,
            SourceOperationLineId = request.SourceOperationLineId,
            UserId = request.UserId,
            Notes = "Item added to package"
        });
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Item {ItemCode} added to package {Barcode}: {Quantity} {UnitCode}", 
            request.ItemCode, package.Barcode, request.Quantity, request.UnitCode);
        
        return content;
    }
    
    public async Task<PackageContent> RemoveItemFromPackageAsync(RemoveItemFromPackageRequest request)
    {
        var package = await GetPackageAsync(request.PackageId);
        if (package == null)
        {
            throw new NotFoundException($"Package {request.PackageId} not found");
        }
        
        if (package.Status == PackageStatus.Locked)
        {
            throw new InvalidOperationException($"Package {package.Barcode} is locked");
        }
        
        var content = await _context.PackageContents
            .FirstOrDefaultAsync(c => c.PackageId == request.PackageId && 
                                     c.ItemCode == request.ItemCode &&
                                     c.BatchNo == request.BatchNo &&
                                     c.SerialNo == request.SerialNo);
        
        if (content == null)
        {
            throw new NotFoundException($"Item {request.ItemCode} not found in package {package.Barcode}");
        }
        
        if (content.Quantity < request.Quantity)
        {
            throw new InvalidOperationException($"Insufficient quantity. Available: {content.Quantity}, Requested: {request.Quantity}");
        }
        
        content.Quantity -= request.Quantity;
        
        // Remove content record if quantity becomes zero
        if (content.Quantity == 0)
        {
            _context.PackageContents.Remove(content);
        }
        
        // Log transaction
        await LogPackageTransactionAsync(new LogPackageTransactionRequest
        {
            PackageId = request.PackageId,
            TransactionType = PackageTransactionType.Remove,
            ItemCode = request.ItemCode,
            Quantity = -request.Quantity, // Negative for removal
            UnitCode = content.UnitCode,
            BatchNo = request.BatchNo,
            SerialNo = request.SerialNo,
            SourceOperationType = request.SourceOperationType,
            SourceOperationId = request.SourceOperationId,
            UserId = request.UserId,
            Notes = "Item removed from package"
        });
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Item {ItemCode} removed from package {Barcode}: {Quantity} {UnitCode}", 
            request.ItemCode, package.Barcode, request.Quantity, content.UnitCode);
        
        return content;
    }
    
    public async Task<string> GeneratePackageBarcodeAsync()
    {
        var settings = GetPackageBarcodeSettings();
        var lastNumber = await GetLastPackageNumberAsync();
        var nextNumber = lastNumber + 1;
        
        var numberPart = nextNumber.ToString().PadLeft(
            settings.Length - settings.Prefix.Length - settings.Suffix.Length, '0');
        
        return $"{settings.Prefix}{numberPart}{settings.Suffix}";
    }
    
    private async Task<long> GetLastPackageNumberAsync()
    {
        var settings = GetPackageBarcodeSettings();
        var prefix = settings.Prefix;
        var suffix = settings.Suffix;
        
        var lastPackage = await _context.Packages
            .Where(p => p.Barcode.StartsWith(prefix) && p.Barcode.EndsWith(suffix))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
        
        if (lastPackage == null)
        {
            return settings.StartNumber - 1;
        }
        
        // Extract number from barcode
        var numberPart = lastPackage.Barcode.Substring(prefix.Length, 
            lastPackage.Barcode.Length - prefix.Length - suffix.Length);
        
        if (long.TryParse(numberPart, out var number))
        {
            return number;
        }
        
        return settings.StartNumber - 1;
    }
    
    private PackageBarcodeSettings GetPackageBarcodeSettings()
    {
        return _configuration.GetSection("Package:Barcode").Get<PackageBarcodeSettings>() 
            ?? new PackageBarcodeSettings
            {
                Prefix = "PKG",
                Length = 14,
                Suffix = "",
                StartNumber = 1
            };
    }
    
    private bool IsPackageFeatureEnabled()
    {
        return _configuration.GetValue<bool>("Options:enablePackages", false);
    }
    
    private string SerializeCustomAttributes(Dictionary<string, object> attributes)
    {
        if (attributes == null || !attributes.Any())
            return null;
            
        return JsonSerializer.Serialize(attributes);
    }
    
    private async Task LogLocationMovementAsync(Guid packageId, PackageMovementType movementType,
        string fromWhsCode, int? fromBinEntry, string fromBinCode,
        string toWhsCode, int? toBinEntry, string toBinCode,
        string sourceOperationType, Guid? sourceOperationId, string userId)
    {
        var movement = new PackageLocationHistory
        {
            Id = Guid.NewGuid(),
            PackageId = packageId,
            MovementType = movementType,
            FromWhsCode = fromWhsCode,
            FromBinEntry = fromBinEntry,
            FromBinCode = fromBinCode,
            ToWhsCode = toWhsCode,
            ToBinEntry = toBinEntry,
            ToBinCode = toBinCode,
            SourceOperationType = sourceOperationType,
            SourceOperationId = sourceOperationId,
            UserId = userId,
            MovementDate = DateTime.UtcNow
        };
        
        _context.PackageLocationHistory.Add(movement);
    }
    
    public async Task LogPackageTransactionAsync(LogPackageTransactionRequest request)
    {
        var transaction = new PackageTransaction
        {
            Id = Guid.NewGuid(),
            PackageId = request.PackageId,
            TransactionType = request.TransactionType,
            ItemCode = request.ItemCode,
            Quantity = request.Quantity,
            UnitCode = request.UnitCode,
            BatchNo = request.BatchNo,
            SerialNo = request.SerialNo,
            SourceOperationType = request.SourceOperationType,
            SourceOperationId = request.SourceOperationId,
            SourceOperationLineId = request.SourceOperationLineId,
            UserId = request.UserId,
            TransactionDate = DateTime.UtcNow,
            Notes = request.Notes
        };
        
        _context.PackageTransactions.Add(transaction);
    }
}
```

## 2.3 Package Controller

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PackageController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly ILogger<PackageController> _logger;
    private readonly IConfiguration _configuration;
    
    public PackageController(
        IPackageService packageService, 
        ILogger<PackageController> logger,
        IConfiguration configuration)
    {
        _packageService = packageService;
        _logger = logger;
        _configuration = configuration;
    }
    
    [HttpPost]
    public async Task<ActionResult<PackageDto>> CreatePackage([FromBody] CreatePackageRequest request)
    {
        try
        {
            request.UserId = EmployeeID;
            var package = await _packageService.CreatePackageAsync(request);
            return Ok(package.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating package");
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<PackageDto>> GetPackage(Guid id)
    {
        var package = await _packageService.GetPackageAsync(id);
        if (package == null)
            return NotFound();
            
        return Ok(package.ToDto());
    }
    
    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<PackageDto>> GetPackageByBarcode(string barcode)
    {
        var package = await _packageService.GetPackageByBarcodeAsync(barcode);
        if (package == null)
            return NotFound();
            
        return Ok(package.ToDto());
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PackageDto>>> GetActivePackages([FromQuery] string whsCode = null)
    {
        var packages = await _packageService.GetActivePackagesAsync(whsCode);
        return Ok(packages.Select(p => p.ToDto()));
    }
    
    [HttpPost("{id}/close")]
    public async Task<ActionResult<PackageDto>> ClosePackage(Guid id)
    {
        try
        {
            var package = await _packageService.ClosePackageAsync(id, EmployeeID);
            
            // Trigger label printing if enabled
            if (IsLabelPrintingEnabled())
            {
                await TriggerLabelPrintingAsync(package);
            }
            
            return Ok(package.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<PackageDto>> CancelPackage(Guid id, [FromBody] CancelPackageRequest request)
    {
        try
        {
            var package = await _packageService.CancelPackageAsync(id, EmployeeID, request.Reason);
            return Ok(package.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("{id}/lock")]
    public async Task<ActionResult<PackageDto>> LockPackage(Guid id, [FromBody] LockPackageRequest request)
    {
        try
        {
            var package = await _packageService.LockPackageAsync(id, request.Reason);
            return Ok(package.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("{id}/unlock")]
    public async Task<ActionResult<PackageDto>> UnlockPackage(Guid id)
    {
        try
        {
            var package = await _packageService.UnlockPackageAsync(id, EmployeeID);
            return Ok(package.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("{id}/contents")]
    public async Task<ActionResult<PackageContentDto>> AddItemToPackage(Guid id, [FromBody] AddItemToPackageRequest request)
    {
        try
        {
            request.PackageId = id;
            request.UserId = EmployeeID;
            var content = await _packageService.AddItemToPackageAsync(request);
            return Ok(content.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpDelete("{id}/contents")]
    public async Task<ActionResult<PackageContentDto>> RemoveItemFromPackage(Guid id, [FromBody] RemoveItemFromPackageRequest request)
    {
        try
        {
            request.PackageId = id;
            request.UserId = EmployeeID;
            var content = await _packageService.RemoveItemFromPackageAsync(request);
            return Ok(content.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from package {PackageId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("{id}/contents")]
    public async Task<ActionResult<IEnumerable<PackageContentDto>>> GetPackageContents(Guid id)
    {
        var contents = await _packageService.GetPackageContentsAsync(id);
        return Ok(contents.Select(c => c.ToDto()));
    }
    
    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<IEnumerable<PackageTransactionDto>>> GetPackageTransactions(Guid id)
    {
        var transactions = await _packageService.GetPackageTransactionHistoryAsync(id);
        return Ok(transactions.Select(t => t.ToDto()));
    }
    
    [HttpGet("{id}/movements")]
    public async Task<ActionResult<IEnumerable<PackageLocationHistoryDto>>> GetPackageMovements(Guid id)
    {
        var movements = await _packageService.GetPackageLocationHistoryAsync(id);
        return Ok(movements.Select(m => m.ToDto()));
    }
    
    [HttpPost("validate-consistency")]
    public async Task<ActionResult<IEnumerable<PackageInconsistencyDto>>> ValidateConsistency([FromQuery] string whsCode = null)
    {
        var inconsistencies = await _packageService.DetectInconsistenciesAsync(whsCode);
        return Ok(inconsistencies.Select(i => i.ToDto()));
    }
    
    [HttpPost("generate-barcode")]
    public async Task<ActionResult<string>> GenerateBarcode()
    {
        var barcode = await _packageService.GeneratePackageBarcodeAsync();
        return Ok(new { barcode });
    }
    
    private bool IsLabelPrintingEnabled()
    {
        return _configuration.GetValue<bool>("Package:Label:AutoPrint", false);
    }
    
    private async Task TriggerLabelPrintingAsync(Package package)
    {
        // Implementation depends on printing infrastructure
        // Could be direct printer integration or queue-based system
        _logger.LogInformation("Triggering label print for package {Barcode}", package.Barcode);
        // await _labelPrintService.PrintPackageLabelAsync(package);
    }
}
```

## 2.4 Request/Response Models

```csharp
public class CreatePackageRequest
{
    public string WhsCode { get; set; }
    public int? BinEntry { get; set; }
    public string BinCode { get; set; }
    public string UserId { get; set; }
    public string SourceOperationType { get; set; }
    public Guid? SourceOperationId { get; set; }
    public Dictionary<string, object> CustomAttributes { get; set; } = new();
}

public class AddItemToPackageRequest
{
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
    public string UserId { get; set; }
    public string SourceOperationType { get; set; }
    public Guid? SourceOperationId { get; set; }
    public Guid? SourceOperationLineId { get; set; }
}

public class RemoveItemFromPackageRequest
{
    public Guid PackageId { get; set; }
    public string ItemCode { get; set; }
    public decimal Quantity { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
    public string UserId { get; set; }
    public string SourceOperationType { get; set; }
    public Guid? SourceOperationId { get; set; }
}

public class PackageDto
{
    public Guid Id { get; set; }
    public string Barcode { get; set; }
    public string Status { get; set; }
    public string WhsCode { get; set; }
    public int? BinEntry { get; set; }
    public string BinCode { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string ClosedBy { get; set; }
    public string Notes { get; set; }
    public Dictionary<string, object> CustomAttributes { get; set; }
    public List<PackageContentDto> Contents { get; set; } = new();
}

public class PackageContentDto
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string WhsCode { get; set; }
    public string BinCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}
```

## 2.5 Extension Methods

```csharp
public static class PackageExtensions
{
    public static PackageDto ToDto(this Package package)
    {
        return new PackageDto
        {
            Id = package.Id,
            Barcode = package.Barcode,
            Status = package.Status.ToString(),
            WhsCode = package.WhsCode,
            BinEntry = package.BinEntry,
            BinCode = package.BinCode,
            CreatedBy = package.CreatedBy,
            CreatedAt = package.CreatedAt,
            ClosedAt = package.ClosedAt,
            ClosedBy = package.ClosedBy,
            Notes = package.Notes,
            CustomAttributes = ParseCustomAttributes(package.CustomAttributes),
            Contents = package.Contents?.Select(c => c.ToDto()).ToList() ?? new List<PackageContentDto>()
        };
    }
    
    public static PackageContentDto ToDto(this PackageContent content)
    {
        return new PackageContentDto
        {
            Id = content.Id,
            PackageId = content.PackageId,
            ItemCode = content.ItemCode,
            Quantity = content.Quantity,
            UnitCode = content.UnitCode,
            BatchNo = content.BatchNo,
            SerialNo = content.SerialNo,
            ExpiryDate = content.ExpiryDate,
            WhsCode = content.WhsCode,
            BinCode = content.BinCode,
            CreatedAt = content.CreatedAt,
            CreatedBy = content.CreatedBy
        };
    }
    
    private static Dictionary<string, object> ParseCustomAttributes(string customAttributesJson)
    {
        if (string.IsNullOrEmpty(customAttributesJson))
            return new Dictionary<string, object>();
            
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(customAttributesJson) 
                ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}
```

## 2.6 Dependency Injection Setup

```csharp
// In Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register package services
    services.AddScoped<IPackageService, PackageService>();
    
    // Register package configuration
    services.Configure<PackageConfiguration>(Configuration.GetSection("Package"));
    services.Configure<PackageBarcodeSettings>(Configuration.GetSection("Package:Barcode"));
    services.Configure<PackageLabelSettings>(Configuration.GetSection("Package:Label"));
    services.Configure<PackageValidationSettings>(Configuration.GetSection("Package:Validation"));
}
```

## Implementation Checklist

### Package Service Interface ✅
- [x] IPackageService interface with comprehensive operations
- [x] Core package lifecycle operations (create, get, close, cancel, lock/unlock)
- [x] Content management operations (add/remove items)
- [x] Location management and movement tracking
- [x] Validation and consistency checking operations
- [x] Barcode generation and validation
- [x] Transaction history and audit logging

### Package Service Implementation ✅
- [x] PackageService class implementing IPackageService
- [x] Complete CRUD operations with business logic validation
- [x] Package status management and lifecycle enforcement
- [x] Location consistency validation
- [x] Configurable barcode generation with customizable format
- [x] Transaction logging for all package operations
- [x] Error handling and validation with descriptive messages
- [x] Database context integration via ILWDbContext

### Package REST API Controller ✅
- [x] PackageController with comprehensive REST endpoints
- [x] Package lifecycle endpoints (create, get, close, cancel, lock/unlock)
- [x] Content management endpoints (add/remove items)
- [x] Query endpoints (active packages, contents, transactions, movements)
- [x] Validation and consistency check endpoints
- [x] Barcode generation endpoint
- [x] Authentication and authorization integration
- [x] Proper error handling and response formatting

### Data Transfer Objects (DTOs) ✅
- [x] Complete request DTOs for all operations
- [x] Response DTOs with proper data mapping
- [x] Extension methods for entity-to-DTO conversion
- [x] Validation result DTOs for consistency checking
- [x] Support for custom attributes and JSON serialization

### Dependency Injection & Configuration ✅
- [x] Service registration in DI container
- [x] ILWDbContext interface and SystemDbContext implementation
- [x] Configuration support for package settings
- [x] Barcode generation configuration options
- [x] Package feature toggle support

### Implementation Notes

**✅ COMPLETED - Phase 2** (59d31d2)
- Full package management service with 20+ operations implemented
- Complete REST API with 15+ endpoints for all package operations
- Comprehensive business logic with validation and error handling
- Configurable barcode generation system
- Transaction logging and audit trail for all operations
- Location consistency validation and movement tracking
- **Enhanced with ObjectType enum integration and role-based authorization**
- **SessionInfo pattern implementation for consistent authentication**
- **Warehouse-aware operations with proper security filtering**

### Key Features Delivered

#### Core Package Management
- **Package Lifecycle Management**: Create, activate, close, cancel, and lock packages
- **Content Management**: Add/remove items with batch/serial number support  
- **Location Tracking**: Full movement history with warehouse and bin tracking
- **Barcode System**: Configurable generation with prefix/suffix/numbering
- **Validation**: Package consistency checks and inconsistency detection
- **Audit Trail**: Complete transaction history for all operations

#### Enhanced Security & Authorization  
- **ObjectType Integration**: Replaced string SourceOperationType with ObjectType enum
- **Role-Based Authorization**: Added PackageManagement and PackageManagementSupervisor roles
- **Dynamic Role Checking**: Operation-type-specific role requirements (GoodsReceipt, Transfer, Picking, Package)
- **SessionInfo Pattern**: Consistent authentication using HttpContext.GetSession() like other controllers
- **Warehouse Security**: Package operations filtered by user's assigned warehouse

#### Modern Architecture Patterns
- **Entity Framework Core**: Updated entities to use Guid for user references
- **Primary Constructor Syntax**: Modern C# 12 patterns in service implementation
- **Comprehensive DTOs**: Complete request/response models with enum integration
- **Extension Methods**: Seamless entity-to-DTO conversion with proper enum handling
- **Dependency Injection**: Proper service registration following existing patterns

### Technical Enhancements Made
1. **ObjectType.Package = 4** added to enum for package operations
2. **PackageTransaction.SourceOperationType** changed from `string` to `ObjectType`
3. **PackageLocationHistory.SourceOperationType** changed from `string` to `ObjectType`
4. **RoleType enum** extended with `PackageManagement = 12` and `PackageManagementSupervisor = 13`
5. **PackageController** updated with `[RequireAnyRole]` attributes and SessionInfo pattern
6. **All DTOs** updated to use `ObjectType?` instead of `string` for operation types
7. **Entity user fields** changed from `string` to `Guid` for better data integrity

### Next Steps
- **Phase 3**: Integration with existing operation controllers (Goods Receipt, Picking, Transfer)
- Package integration with existing SAP B1 workflows
- Enhanced business rules and validation integration
- Implementation of package movement with SAP stock transfer integration