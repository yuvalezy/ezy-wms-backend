namespace Core.DTOs.PickList;

/// <summary>
/// Contains information about why a pick list was closed in the external system
/// </summary>
public class PickListClosureInfo {
    /// <summary>
    /// The pick list absolute entry
    /// </summary>
    public int AbsEntry { get; set; }
    /// <summary>
    /// The pick list pick entry
    /// </summary>
    public int PickEntry { get; set; }
    
    /// <summary>
    /// Whether the pick list is closed in the external system
    /// </summary>
    public bool IsClosed { get; set; }
    
    /// <summary>
    /// The reason for closure (e.g., "Manual", "FollowUpDocument", "Cancelled")
    /// </summary>
    public string ClosureReason { get; set; } = string.Empty;
    
    /// <summary>
    /// Date when the pick list was closed
    /// </summary>
    public DateTime? ClosedDate { get; set; }
    
    /// <summary>
    /// List of follow-up documents created from this pick list
    /// </summary>
    public List<FollowUpDocumentInfo> FollowUpDocuments { get; set; } = new();
    
    /// <summary>
    /// Indicates if package movements should be processed
    /// </summary>
    public bool RequiresPackageMovement => IsClosed && ClosureReason == "FollowUpDocument" && FollowUpDocuments.Any();
}