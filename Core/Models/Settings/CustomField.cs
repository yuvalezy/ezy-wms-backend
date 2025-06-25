using System.Text.Json.Serialization;

namespace Core.Models.Settings;

public class CustomField {
    public required string Key { get; set; }

    [JsonIgnore]
    public string Query { get; set; } = string.Empty;

    public required string          Description { get; set; }
    public required CustomFieldType Type        { get; set; }
}

public enum CustomFieldType {
    Text,
    Number,
    Date
}