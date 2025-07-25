namespace Core.Models.Settings;

/// <summary>
/// Settings for external commands system
/// </summary>
public class ExternalCommandsSettings {
    /// <summary>
    /// List of configured external commands
    /// </summary>
    public ExternalCommand[] Commands { get; set; } = [];
    
    /// <summary>
    /// Global settings for the external commands system
    /// </summary>
    public ExternalCommandsGlobalSettings GlobalSettings { get; set; } = new();
}