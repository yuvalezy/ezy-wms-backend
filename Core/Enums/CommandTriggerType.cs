namespace Core.Enums;

/// <summary>
/// Defines when an external command should be triggered
/// </summary>
public enum CommandTriggerType {
    /// <summary>
    /// Triggered when a package is created
    /// </summary>
    CreatePackage = 0,
    
    /// <summary>
    /// Triggered when a package is closed
    /// </summary>
    ClosePackage = 1,
    
    /// <summary>
    /// Manually triggered by user action
    /// </summary>
    Manual = 2
}