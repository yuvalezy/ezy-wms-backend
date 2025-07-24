using System.ComponentModel.DataAnnotations;
using Core.DTOs.Package;
using Core.Entities;
using Core.Enums;
using Core.Models;
using Core.Models.Settings;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Core.Interfaces;

namespace Infrastructure.Services;

public class PackageService(
    SystemDbContext           db,
    ISettings                 settings,
    ILogger<PackageService>   logger,
    IPackageContentService    contentService,
    IPackageValidationService validationService,
    IPackageLocationService   locationService) : IPackageService {
    public async Task<Package> CreatePackageAsync(SessionInfo sessionInfo, CreatePackageRequest request) {
        if (!settings.Options.EnablePackages) {
            throw new InvalidOperationException("Package feature is not enabled");
        }

        string barcode = await validationService.GeneratePackageBarcodeAsync();

        string whsCode = sessionInfo.Warehouse;
        var    userId  = sessionInfo.Guid;
        var package = new Package {
            Id                  = Guid.NewGuid(),
            Barcode             = barcode,
            Status              = PackageStatus.Init,
            WhsCode             = whsCode,
            BinEntry            = request.BinEntry,
            CreatedByUserId     = userId,
            ClosedAt            = null,
            ClosedBy            = null,
            Notes               = null,
            SourceOperationType = request.SourceOperationType ?? ObjectType.Package,
            SourceOperationId   = request.SourceOperationId,
            CustomAttributes    = SerializeCustomAttributes(request.CustomAttributes)
        };

        db.Packages.Add(package);

        await locationService.LogLocationMovementAsync(package.Id, PackageMovementType.Created,
            null, null, whsCode, request.BinEntry,
            request.SourceOperationType ?? ObjectType.Package, request.SourceOperationId, userId);

        await db.SaveChangesAsync();

        logger.LogInformation("Package created: {Barcode} by user {UserId}", barcode, userId);

        return package;
    }

    public async Task<Package?> GetPackageAsync(Guid packageId) {
        return await db.Packages
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == packageId && !p.Deleted);
    }

    public async Task<Package?> GetPackageByBarcodeAsync(PackageByBarcodeRequest parameters) {
        var query = db.Packages.AsQueryable();
        if (parameters is { Contents: true })
            query = query.Include(c => c.Contents);
        if (parameters is { History: true })
            query = query.Include(c => c.LocationHistory);
        if (parameters is { Details: true })
            query = query.Include(c => c.CreatedByUser);
        
        return await query.FirstOrDefaultAsync(p => p.Barcode == parameters.Barcode && !p.Deleted);
    }

    public async Task<IEnumerable<Package>> GetActivePackagesAsync(string? whsCode = null) {
        var query = db.Packages
            .Where(p => p.Status == PackageStatus.Active && !p.Deleted);

        if (!string.IsNullOrEmpty(whsCode)) {
            query = query.Where(p => p.WhsCode == whsCode);
        }

        return await query.ToListAsync();
    }

    public async Task ActivatePackagesByIdAsync(Guid id, SessionInfo sessionInfo)
    {
        var package = await db.Packages.Include(p => p.Contents).FirstOrDefaultAsync(p => p.Id == id);
        if (package == null || package.WhsCode != sessionInfo.Warehouse)
            throw new ValidationException($"Package warehouse {package?.WhsCode} does not match user warehouse {sessionInfo.Warehouse}");

        if (package.Status != PackageStatus.Init || package.Contents.Count <= 0)
            throw new ValidationException($"Package {package.Barcode} is not active or has no items");

        package.Status = PackageStatus.Active;
        package.UpdatedAt = DateTime.UtcNow;
        package.UpdatedByUserId = sessionInfo.Guid;

        await db.SaveChangesAsync();

        logger.LogInformation("Package {Barcode} activated by user {UserId}", package.Barcode, sessionInfo.Guid);
    }

    public async Task<int> ActivatePackagesBySourceAsync(ObjectType sourceOperationType, Guid sourceOperationId, SessionInfo sessionInfo) {
        var packages     = await db.Packages.Where(v => v.SourceOperationId == sourceOperationId).Include(package => package.Contents).ToListAsync();
        var initPackages = packages.Where(p => p.Status == PackageStatus.Init).ToList();

        int activatedCount = 0;
        foreach (var package in initPackages) {
            if (package.Contents.Count <= 0)
                continue;
            package.Status          = PackageStatus.Active;
            package.UpdatedAt       = DateTime.UtcNow;
            package.UpdatedByUserId = sessionInfo.Guid;
            activatedCount++;

            logger.LogInformation("Package {Barcode} activated for {SourceOperationType} operation {SourceOperationId}",
                package.Barcode, sourceOperationType, sourceOperationId);
        }

        return activatedCount;
    }

    public async Task<Package> ClosePackageAsync(Guid packageId, SessionInfo sessionInfo) {
        var package = await GetPackageAsync(packageId);
        if (package == null || package.WhsCode != sessionInfo.Warehouse) {
            throw new InvalidOperationException($"Package {packageId} not found");
        }

        if (package.Status != PackageStatus.Active) {
            throw new InvalidOperationException($"Package {package.Barcode} is not active");
        }

        if (package.Contents.Count > 0) {
            throw new InvalidOperationException($"Package {package.Barcode} has items");
        }

        package.Status          = PackageStatus.Closed;
        package.ClosedAt        = DateTime.UtcNow;
        package.ClosedBy        = sessionInfo.Guid;
        package.UpdatedAt       = DateTime.UtcNow;
        package.UpdatedByUserId = sessionInfo.Guid;

        await db.SaveChangesAsync();

        logger.LogInformation("Package closed: {Barcode} by user {UserId}", package.Barcode, sessionInfo);

        return package;
    }

    public async Task<Package> CancelPackageAsync(Guid packageId, SessionInfo sessionInfo, string? reason) {
        var package = await GetPackageAsync(packageId);
        if (package == null || sessionInfo.Warehouse != package.WhsCode) {
            throw new InvalidOperationException($"Package {packageId} not found");
        }

        if (package.Status != PackageStatus.Init && package.Contents.Count > 0) {
            throw new InvalidOperationException($"Package {package.Barcode} has items");
        }

        package.Status          = PackageStatus.Cancelled;
        package.ClosedAt        = DateTime.UtcNow;
        package.ClosedBy        = sessionInfo.Guid;
        package.Notes           = reason;
        package.UpdatedAt       = DateTime.UtcNow;
        package.UpdatedByUserId = sessionInfo.Guid;

        await db.SaveChangesAsync();

        logger.LogInformation("Package cancelled: {Barcode} by user {UserId} - Reason: {Reason}",
            package.Barcode, sessionInfo, reason);

        return package;
    }

    public async Task<Package> LockPackageAsync(Guid packageId, SessionInfo sessionInfo, string? reason) {
        var package = await GetPackageAsync(packageId);
        if (package == null || sessionInfo.Warehouse != package.WhsCode) {
            throw new InvalidOperationException($"Package {packageId} not found");
        }

        package.Status          = PackageStatus.Locked;
        package.Notes           = reason;
        package.UpdatedAt       = DateTime.UtcNow;
        package.UpdatedByUserId = sessionInfo.Guid;

        await db.SaveChangesAsync();

        logger.LogWarning("Package locked: {Barcode} - Reason: {Reason}", package.Barcode, reason);

        return package;
    }

    public async Task<Package> UnlockPackageAsync(Guid packageId, SessionInfo sessionInfo) {
        var package = await GetPackageAsync(packageId);
        if (package == null) {
            throw new InvalidOperationException($"Package {packageId} not found");
        }

        if (package.Status != PackageStatus.Locked) {
            throw new InvalidOperationException($"Package {package.Barcode} is not locked");
        }

        package.Status          = PackageStatus.Active;
        package.Notes           = null;
        package.UpdatedAt       = DateTime.UtcNow;
        package.UpdatedByUserId = sessionInfo.Guid;

        await db.SaveChangesAsync();

        logger.LogInformation("Package unlocked: {Barcode} by user {UserId}", package.Barcode, sessionInfo);

        return package;
    }

    // Content Management - used internally and by GoodsReceiptService
    public async Task<PackageContent> AddItemToPackageAsync(AddItemToPackageRequest request, SessionInfo sessionInfo) {
        return await contentService.AddItemToPackageAsync(request, sessionInfo);
    }

    /// <summary>
    /// Updates metadata for an existing package
    /// </summary>
    /// <param name="packageId">Package ID to update</param>
    /// <param name="request">Metadata update request</param>
    /// <param name="sessionInfo">Current user session</param>
    /// <returns>Updated package entity</returns>
    /// <exception cref="ArgumentException">If validation fails</exception>
    /// <exception cref="InvalidOperationException">If package not found or not accessible</exception>
    public async Task<Package> UpdatePackageMetadataAsync(
        Guid packageId, 
        UpdatePackageMetadataRequest request, 
        SessionInfo sessionInfo) {
        
        // Get existing package
        var package = await db.Packages.FirstOrDefaultAsync(p => p.Id == packageId);
        if (package == null) {
            throw new InvalidOperationException($"Package not found: {packageId}");
        }
        
        // Check warehouse access
        if (package.WhsCode != sessionInfo.Warehouse) {
            throw new InvalidOperationException("Package not accessible in current warehouse");
        }
        
        // Validate metadata against configured definitions
        var metadataDefinitions = settings.Package.MetadataDefinition;
        var validationErrors = ValidateMetadata(request.Metadata, metadataDefinitions);
        
        if (validationErrors.Any()) {
            throw new ArgumentException($"Metadata validation failed: {string.Join("; ", validationErrors)}");
        }
        
        // Merge with existing metadata
        var existingMetadata = ParseCustomAttributes(package.CustomAttributes);
        
        // Update with new values
        foreach (var kvp in request.Metadata) {
            if (kvp.Value == null) {
                existingMetadata.Remove(kvp.Key);
            } else {
                // Extract actual value from JsonElement if needed
                var actualValue = ExtractValue(kvp.Value);
                existingMetadata[kvp.Key] = actualValue;
            }
        }
        
        // Serialize and save
        package.CustomAttributes = SerializeCustomAttributes(existingMetadata);
        package.UpdatedAt = DateTime.UtcNow;
        package.UpdatedByUserId = sessionInfo.Guid;
        
        await db.SaveChangesAsync();
        
        return package;
    }

    /// <summary>
    /// Validates metadata values against configured definitions
    /// </summary>
    /// <param name="metadata">Metadata key-value pairs to validate</param>
    /// <param name="metadataDefinitions">Configured metadata definitions</param>
    /// <returns>Validation errors, empty if valid</returns>
    private IEnumerable<string> ValidateMetadata(
        Dictionary<string, object?> metadata, 
        PackageMetadataDefinition[] metadataDefinitions) {
        
        var errors = new List<string>();
        
        foreach (var kvp in metadata) {
            var definition = metadataDefinitions.FirstOrDefault(d => 
                string.Equals(d.Id, kvp.Key, StringComparison.OrdinalIgnoreCase));
            
            if (definition == null) {
                errors.Add($"Unknown metadata field: {kvp.Key}");
                continue;
            }
            
            if (kvp.Value == null) {
                // Null values are allowed for all fields (optional)
                continue;
            }
            
            var validationError = ValidateMetadataFieldValue(kvp.Value, definition);
            if (validationError != null) {
                errors.Add($"Invalid value for {kvp.Key}: {validationError}");
            }
        }
        
        return errors;
    }

    /// <summary>
    /// Validates a single metadata field value against its definition
    /// </summary>
    private string? ValidateMetadataFieldValue(object value, PackageMetadataDefinition definition) {
        try {
            return definition.Type switch {
                MetadataFieldType.String => ValidateStringValue(value),
                MetadataFieldType.Decimal => ValidateDecimalValue(value), 
                MetadataFieldType.Date => ValidateDateValue(value),
                _ => $"Unsupported field type: {definition.Type}"
            };
        }
        catch (Exception ex) {
            return ex.Message;
        }
    }

    private string? ValidateStringValue(object value) {
        if (value is string) return null;
        
        if (value is JsonElement element && element.ValueKind == JsonValueKind.String) {
            return null;
        }
        
        return $"Expected string value, got {value.GetType().Name}";
    }

    private string? ValidateDecimalValue(object value) {
        // Handle both direct decimal and JsonElement cases
        if (value is decimal) return null;
        if (value is double d) return null;
        if (value is int i) return null;
        
        if (value is JsonElement element) {
            if (element.TryGetDecimal(out _)) return null;
            if (element.TryGetDouble(out _)) return null;
            if (element.TryGetInt32(out _)) return null;
        }
        
        if (decimal.TryParse(value.ToString(), out _)) return null;
        
        return $"Expected decimal value, got {value.GetType().Name}";
    }

    private string? ValidateDateValue(object value) {
        if (value is DateTime) return null;
        if (value is DateOnly) return null;
        
        if (value is JsonElement element && element.TryGetDateTime(out _)) return null;
        
        if (DateTime.TryParse(value.ToString(), out _)) return null;
        
        return $"Expected date value, got {value.GetType().Name}";
    }

    private object ExtractValue(object value) {
        if (value is JsonElement element) {
            return element.ValueKind switch {
                JsonValueKind.String => element.GetString()!,
                JsonValueKind.Number => element.TryGetDecimal(out var dec) ? dec : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                _ => element.ToString()
            };
        }
        return value;
    }

    private Dictionary<string, object> ParseCustomAttributes(string? customAttributesJson) {
        if (string.IsNullOrEmpty(customAttributesJson)) {
            return new Dictionary<string, object>();
        }

        try {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(customAttributesJson) 
                   ?? new Dictionary<string, object>();
        }
        catch {
            return new Dictionary<string, object>();
        }
    }

    private string? SerializeCustomAttributes(Dictionary<string, object>? attributes) {
        if (attributes == null || !attributes.Any())
            return null;

        return JsonSerializer.Serialize(attributes);
    }
}