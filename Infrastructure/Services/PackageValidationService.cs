using Core.DTOs.Package;
using Core.Entities;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PackageValidationService(SystemDbContext context, ISettings settings, ILogger<PackageValidationService> logger) : IPackageValidationService {
    
    public async Task<PackageValidationResult> ValidatePackageConsistencyAsync(Guid packageId) {
        var package = await context.Packages
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == packageId && !p.Deleted);
            
        if (package == null) {
            return new PackageValidationResult {
                IsValid = false,
                Errors  = ["Package not found"]
            };
        }

        var result = new PackageValidationResult { IsValid = true };

        var contents                = package.Contents.Where(c => !c.Deleted);
        var locationInconsistencies = contents.Where(c => c.WhsCode != package.WhsCode || c.BinEntry != package.BinEntry).ToList();

        if (locationInconsistencies.Any()) {
            result.IsValid = false;
            result.Errors.Add($"Location inconsistency: {locationInconsistencies.Count} items have different location than package");
        }

        // Additional validation rules can be added here
        // - Package weight validation
        // - Item compatibility validation
        // - Business rule validation
        
        logger.LogDebug("Package {PackageId} validation result: {IsValid}", packageId, result.IsValid);
        
        return result;
    }

    public async Task<IEnumerable<PackageInconsistency>> DetectInconsistenciesAsync(string? whsCode = null) {
        var query = context.PackageInconsistencies
            .Where(i => !i.IsResolved && !i.Deleted);

        if (!string.IsNullOrEmpty(whsCode)) {
            query = query.Where(i => i.WhsCode == whsCode);
        }

        var inconsistencies = await query.ToListAsync();
        
        logger.LogInformation("Found {Count} unresolved package inconsistencies in warehouse {WhsCode}", 
            inconsistencies.Count(), whsCode ?? "ALL");
            
        return inconsistencies;
    }

    public async Task<string> GeneratePackageBarcodeAsync() {
        var  barcodeSettings = settings.Package.Barcode;
        long lastNumber      = await GetLastPackageNumberAsync();
        long nextNumber      = lastNumber + 1;

        string numberPart = nextNumber.ToString().PadLeft(
            barcodeSettings.Length - barcodeSettings.Prefix.Length - barcodeSettings.Suffix.Length, '0');

        var barcode = $"{barcodeSettings.Prefix}{numberPart}{barcodeSettings.Suffix}";
        
        logger.LogDebug("Generated package barcode: {Barcode} (number: {Number})", barcode, nextNumber);
        
        return barcode;
    }

    public async Task<bool> ValidatePackageBarcodeAsync(string barcode) {
        if (string.IsNullOrEmpty(barcode))
            return false;

        var existing = await context.Packages
            .FirstOrDefaultAsync(p => p.Barcode == barcode && !p.Deleted);

        bool isValid = existing == null;
        
        if (!isValid) {
            logger.LogWarning("Barcode validation failed: {Barcode} already exists", barcode);
        }
        
        return isValid;
    }
    
    private async Task<long> GetLastPackageNumberAsync() {
        var     barcodeSettings = settings.Package.Barcode;
        string? prefix          = barcodeSettings.Prefix;
        string? suffix          = barcodeSettings.Suffix;

        var lastPackage = await context.Packages
            .Where(p => p.Barcode.StartsWith(prefix) && p.Barcode.EndsWith(suffix) && !p.Deleted)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastPackage == null) {
            return barcodeSettings.StartNumber - 1;
        }

        string numberPart = lastPackage.Barcode.Substring(prefix.Length, lastPackage.Barcode.Length - prefix.Length - suffix.Length);

        if (long.TryParse(numberPart, out long number)) {
            return number;
        }

        return barcodeSettings.StartNumber - 1;
    }
}