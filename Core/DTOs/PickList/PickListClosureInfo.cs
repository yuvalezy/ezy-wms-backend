namespace Core.DTOs.PickList;

/// <summary>
/// Contains information about why a pick list was closed in the external system
/// </summary>
public class PickListClosureInfo {
    /// <summary>
    /// Whether the pick list is closed in the external system
    /// </summary>
    public bool IsClosed { get; set; }
    
    /// <summary>
    /// The reason for closure (e.g., "Manual", "FollowUpDocument", "Cancelled")
    /// </summary>
    public PickListClosureReasonType ClosureReason { get; set; } 
    
    /// <summary>
    /// List of follow-up documents created from this pick list
    /// </summary>
    public List<FollowUpDocumentInfo> FollowUpDocuments { get; set; } = [];
    
    /// <summary>
    /// Indicates if package movements should be processed
    /// </summary>
    public bool RequiresPackageMovement => IsClosed && ClosureReason == PickListClosureReasonType.FollowUpDocument && FollowUpDocuments.Count > 0;
}

public enum PickListClosureReasonType {
    FollowUpDocument,
    Closed
}