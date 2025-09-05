namespace Core.Models.Settings;

public class PickingPostProcessorSettings {
    public required string Id { get; set; }
    public required string Assembly { get; set; }
    public required string TypeName { get; set; }
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object>? Configuration { get; set; }
}
