using Core.DTOs.Package;
using Core.Enums;

namespace Core.DTOs.PickList;

public class PickListPackageResponse {
    /// <summary>
    /// The response status
    /// </summary>
    public ResponseStatus Status { get; set; } = ResponseStatus.Ok;
    
    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// The IDs of the created pick list entries
    /// </summary>
    public Guid[] PickListIds { get; set; } = [];
    
    /// <summary>
    /// The package ID that was processed
    /// </summary>
    public Guid PackageId { get; set; }
    
    /// <summary>
    /// The contents of the package that were added
    /// </summary>
    public List<PackageContentDto> PackageContents { get; set; } = new();
    
    /// <summary>
    /// Creates a successful response with no additional data
    /// </summary>
    public static PickListPackageResponse OkResponse => new() { Status = ResponseStatus.Ok };
    
    /// <summary>
    /// Creates an error response with the specified message
    /// </summary>
    public static PickListPackageResponse ErrorResponse(string message) => new() { 
        Status = ResponseStatus.Error, 
        ErrorMessage = message 
    };
}