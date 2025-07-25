namespace Core.Models.Settings;

/// <summary>
/// Defines UI configuration for manual external commands
/// </summary>
public class CommandUIConfiguration {
    /// <summary>
    /// List of screens where this command can be triggered
    /// </summary>
    public string[] AllowedScreens { get; set; } = [];
    
    /// <summary>
    /// Text to display on the trigger button
    /// </summary>
    public string ButtonText { get; set; } = "Execute Command";
    
    /// <summary>
    /// Whether to require user confirmation before execution
    /// </summary>
    public bool RequireConfirmation { get; set; } = true;
    
    /// <summary>
    /// Confirmation message to display (supports placeholders like {count})
    /// </summary>
    public string? ConfirmationMessage { get; set; }
    
    /// <summary>
    /// Maximum number of items that can be processed in batch
    /// </summary>
    public int? MaxBatchSize { get; set; }
}