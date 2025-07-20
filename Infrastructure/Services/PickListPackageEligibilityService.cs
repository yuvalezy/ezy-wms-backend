using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for checking if packages can be fully picked based on pick list requirements
/// </summary>
public class PickListPackageEligibilityService(ILogger<PickListPackageEligibilityService> logger) {
    
    /// <summary>
    /// Checks if a package can be fully picked given the available open quantities
    /// </summary>
    /// <param name="packageContents">The contents of the package to check</param>
    /// <param name="itemOpenQuantities">Dictionary of item codes to their open quantities</param>
    /// <returns>True if all items in the package can be fully picked</returns>
    public bool CanPackageBeFullyPicked(
        List<PackageContent> packageContents, 
        Dictionary<string, int> itemOpenQuantities) {
        
        foreach (var content in packageContents) {
            // Must have no committed quantity
            if (content.CommittedQuantity > 0) {
                logger.LogDebug("Package cannot be fully picked: Item {ItemCode} has committed quantity {CommittedQuantity}", 
                    content.ItemCode, content.CommittedQuantity);
                return false;
            }
            
            // Must have corresponding item with sufficient open quantity
            if (!itemOpenQuantities.TryGetValue(content.ItemCode, out var openQty) || 
                openQty < content.Quantity) {
                logger.LogDebug("Package cannot be fully picked: Item {ItemCode} requires {Required} but only {Available} available", 
                    content.ItemCode, content.Quantity, openQty);
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets the missing quantities for each item in the package
    /// </summary>
    /// <param name="packageContents">The contents of the package to check</param>
    /// <param name="itemOpenQuantities">Dictionary of item codes to their open quantities</param>
    /// <returns>Dictionary of item codes to their missing quantities (0 if sufficient)</returns>
    public Dictionary<string, decimal> GetMissingQuantities(
        List<PackageContent> packageContents,
        Dictionary<string, int> itemOpenQuantities) {
        
        var missingQuantities = new Dictionary<string, decimal>();
        
        foreach (var content in packageContents) {
            var required = content.Quantity - content.CommittedQuantity;
            var available = itemOpenQuantities.TryGetValue(content.ItemCode, out var openQty) ? openQty : 0;
            var missing = Math.Max(0, required - available);
            
            missingQuantities[content.ItemCode] = missing;
        }
        
        return missingQuantities;
    }
    
    /// <summary>
    /// Validates that a package meets all requirements for being added to a pick list
    /// </summary>
    /// <param name="packageContents">The contents of the package to validate</param>
    /// <param name="itemOpenQuantities">Dictionary of item codes to their open quantities</param>
    /// <param name="errorMessage">Output parameter for validation error message</param>
    /// <returns>True if package is valid for picking</returns>
    public bool ValidatePackageForPicking(
        List<PackageContent> packageContents,
        Dictionary<string, int> itemOpenQuantities,
        out string? errorMessage) {
        
        errorMessage = null;
        
        if (!packageContents.Any()) {
            errorMessage = "Package is empty";
            return false;
        }
        
        // Check for committed quantities
        var itemsWithCommittedQty = packageContents
            .Where(c => c.CommittedQuantity > 0)
            .Select(c => c.ItemCode)
            .ToList();
            
        if (itemsWithCommittedQty.Any()) {
            errorMessage = $"Package has committed quantities for items: {string.Join(", ", itemsWithCommittedQty)}";
            return false;
        }
        
        // Check for missing items or insufficient quantities
        var missingQuantities = GetMissingQuantities(packageContents, itemOpenQuantities);
        var insufficientItems = missingQuantities
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => $"{kvp.Key} (need {kvp.Value} more)")
            .ToList();
            
        if (insufficientItems.Any()) {
            errorMessage = $"Insufficient open quantities for: {string.Join(", ", insufficientItems)}";
            return false;
        }
        
        return true;
    }
}