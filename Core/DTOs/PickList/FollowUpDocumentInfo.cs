namespace Core.DTOs.PickList;

/// <summary>
/// Information about a follow-up document created from a pick list
/// </summary>
public class FollowUpDocumentInfo {
    /// <summary>
    /// The pick list source entry
    /// </summary>
    public int PickEntry { get; set; }

    /// <summary>
    /// The type of document (e.g., 15 = Delivery, 16 = Return)
    /// </summary>
    public int DocumentType { get; set; }

    /// <summary>
    /// The document entry number
    /// </summary>
    public int DocumentEntry { get; set; }

    /// <summary>
    /// The document number (visible number)
    /// </summary>
    public int DocumentNumber { get; set; }

    /// <summary>
    /// Date when the document was created
    /// </summary>
    public DateTime DocumentDate { get; set; }

    /// <summary>
    /// List of items and quantities that were actually delivered/processed
    /// </summary>
    public List<FollowUpDocumentItem> Items { get; set; } = [];
}

/// <summary>
/// Item details from a follow-up document
/// </summary>
public class FollowUpDocumentItem {
    /// <summary>
    /// The item code
    /// </summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// The quantity that was actually delivered/processed
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// The bin location from which the item was picked
    /// </summary>
    public int? BinEntry { get; set; }
}