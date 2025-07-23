using Core.DTOs.Items;
using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Service interface for item metadata operations
/// Provides abstraction layer over external system adapter for item metadata management
/// </summary>
public interface IItemService {
    /// <summary>
    /// Updates metadata for a specific item via external adapter
    /// </summary>
    /// <param name="itemCode">The item code to update</param>
    /// <param name="request">The metadata update request containing field values</param>
    /// <param name="sessionInfo">Current user session information</param>
    /// <returns>Updated item metadata response</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the item is not found in the external system</exception>
    /// <exception cref="ValidationException">Thrown when validation fails (read-only fields, mandatory fields, etc.)</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when user lacks permission to update item metadata</exception>
    Task<ItemMetadataResponse> UpdateItemMetadataAsync(
        string itemCode, 
        UpdateItemMetadataRequest request, 
        SessionInfo sessionInfo);
    
    /// <summary>
    /// Retrieves metadata for a specific item via external adapter
    /// </summary>
    /// <param name="itemCode">The item code to retrieve metadata for</param>
    /// <returns>Item metadata response or null if not found</returns>  
    Task<ItemMetadataResponse?> GetItemMetadataAsync(string itemCode);
}