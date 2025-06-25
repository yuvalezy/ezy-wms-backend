namespace Core.Models.Settings;

public class CustomField {
    public required string          Key         { get; set; }
    public required string          Query       { get; set; }
    public required string          Description { get; set; }
    public required CustomFieldType Type        { get; set; }
}

public enum CustomFieldType {
    Text,
    Number,
    Date
}