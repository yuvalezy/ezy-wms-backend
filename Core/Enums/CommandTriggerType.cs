namespace Core.Enums;

/// <summary>
/// Defines when an external command should be triggered
/// </summary>
public enum CommandTriggerType
{
    /// <summary>
    /// Triggered when a package is created
    /// </summary>
    CreatePackage = 0,

    /// <summary>
    /// Trigger when a package is activated
    /// </summary>
    ActivatePackage = 1,

    /// <summary>
    /// Triggered when a package is closed
    /// </summary>
    ClosePackage = 2,

    /// <summary>
    /// Manually triggered by user action
    /// </summary>
    Manual = 3,
}