namespace Core.Models.Settings;

/// <summary>
/// Defines a custom metadata field that can be configured for items loaded from external systems
/// </summary>
public class MetadataDefinition {
    /// <summary>
    /// Unique identifier for the metadata field (e.g., "ItemCode", "PurchaseUnitVolume", "U_B1SStdTP")
    /// Must match one of the supported item fields exactly
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Human-readable description/label for the field
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Data type for validation and UI rendering
    /// </summary>
    public required MetadataFieldType Type { get; set; }

    /// <summary>
    /// SQL query to retrieve additional data for the metadata field
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// SQL expression for grouping query results 
    /// </summary>
    public string? GroupBy { get; set; }

    /// <summary>
    /// Specifies the step increment for decimal type fields
    /// Only applicable when Type is set to Decimal
    /// </summary>
    public int? Step { get; set; }

    /// <summary>
    /// Whether this field is required when updating item metadata
    /// Cannot be true for read-only fields
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Whether this field is read-only (loaded from external system, cannot be modified)
    /// ItemCode and ItemName are always read-only
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// When set, the same value will also be written to this SAP field during PATCH updates.
    /// Used to mirror Purchase fields to their Sales counterparts (e.g., PurchaseUnitWidth → SalesUnitWidth).
    /// </summary>
    public string? MirrorTo { get; set; }

    /// <summary>
    /// SAP field ID whose value is used as a scale factor for this field.
    /// On load (SAP → WMS): wms_value = sap_value * scale_field_value.
    /// On save (WMS → SAP): sap_value = wms_value / scale_field_value.
    /// The referenced field must also be present in MetadataDefinition so it is included
    /// in the SAP $select and available in the metadata response for formula references.
    /// Example: "PurPackUn"
    /// </summary>
    public string? ScaleByField { get; set; }

    /// <summary>
    /// Optional calculation configuration for computed metadata fields
    /// </summary>
    public Calculated? Calculated { get; set; }

    /// <summary>
    /// Name of a text UDF on the SAP item (e.g. "U_EZY_WMS_DATA") that holds a JSON object.
    /// When set, this field's value is persisted as key <see cref="Id"/> inside that JSON blob
    /// instead of being written to the SAP property named <see cref="Id"/>.
    /// On load the blob wins; if the blob has no entry the SAP property is read as a fallback
    /// (with <see cref="ScaleByField"/> applied). Blob values are never scaled — they are stored
    /// exactly as WMS sees them.
    /// </summary>
    public string? StoreIn { get; set; }

    /// <summary>
    /// Only meaningful together with <see cref="StoreIn"/>. When true the value is ALSO written to the
    /// SAP property named <see cref="Id"/> (scaled by <see cref="ScaleByField"/>, mirrored via
    /// <see cref="MirrorTo"/>) in addition to the JSON blob.
    /// </summary>
    public bool SyncToSap { get; set; }

    /// <summary>
    /// Only meaningful together with <see cref="StoreIn"/> (and not <see cref="SyncToSap"/>). When true,
    /// every time this field is saved the SAP property named <see cref="Id"/> — and its
    /// <see cref="MirrorTo"/> twin — is written as 0 (numeric) / null (other types).
    /// Needed because SAP derives some fields from others (e.g. volume from length × width × height):
    /// as long as SAP still holds non-zero dimensions it ignores any volume we send and recomputes it.
    /// </summary>
    public bool ClearSapField { get; set; }

    /// <summary>
    /// When true the field is never sent to the frontend (not rendered, not editable) but still takes
    /// part in the SAP write. Used for constants such as a volume unit code.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Constant value (parsed according to <see cref="Type"/>) written to SAP on every metadata save.
    /// Fields with a fixed value are never user-editable.
    /// </summary>
    public string? FixedValue { get; set; }
}

/// <summary>
/// Defines calculation settings for computed metadata fields
/// </summary>
public class Calculated {
    /// <summary>
    /// The formula expression used to calculate the field value
    /// </summary>
    public required string Formula { get; set; } = "";

    /// <summary>
    /// Array of field IDs that this calculated field depends on
    /// </summary>
    public string[] Dependencies { get; set; } = [];

    /// <summary>
    /// Number of decimal places to round the calculated result to (for numeric fields)
    /// </summary>
    public int Precision { get; set; } = 0;

    /// <summary>
    /// When true, dependency fields are cleared when the calculated field is manually edited
    /// </summary>
    public bool ClearDependenciesOnManualEdit { get; set; } = false;
}

/// <summary>
/// Supported data types for metadata fields
/// </summary>
public enum MetadataFieldType {
    /// <summary>
    /// Text/string value
    /// </summary>
    String,

    /// <summary>
    /// Decimal number value
    /// </summary>
    Decimal,

    /// <summary>
    /// Date value
    /// </summary>
    Date,

    /// <summary>
    /// Integer number value
    /// </summary>
    Integer
}