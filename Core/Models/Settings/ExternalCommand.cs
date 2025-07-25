using Core.Enums;

namespace Core.Models.Settings;

/// <summary>
/// Defines an external command configuration
/// </summary>
public class ExternalCommand {
    /// <summary>
    /// Unique identifier for the command
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// Human-readable name for the command
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Description of what the command does
    /// </summary>
    public required string Description { get; set; }
    
    /// <summary>
    /// The object type this command applies to
    /// </summary>
    public ObjectType ObjectType { get; set; }
    
    /// <summary>
    /// When this command should be triggered
    /// </summary>
    public CommandTriggerType TriggerType { get; set; }
    
    /// <summary>
    /// Whether this command is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Whether this command supports batch execution for multiple items
    /// </summary>
    public bool AllowBatchExecution { get; set; } = false;
    
    /// <summary>
    /// Queries to execute to gather data
    /// </summary>
    public CommandQuery[] Queries { get; set; } = [];
    
    /// <summary>
    /// File format for the generated output
    /// </summary>
    public CommandFileFormat FileFormat { get; set; }
    
    /// <summary>
    /// Pattern for generating file names (supports placeholders like {Barcode}, {Timestamp})
    /// </summary>
    public required string FileNamePattern { get; set; }
    
    /// <summary>
    /// Destination configuration for file delivery
    /// </summary>
    public required CommandDestination Destination { get; set; }
    
    /// <summary>
    /// UI configuration for manual commands
    /// </summary>
    public CommandUIConfiguration? UIConfiguration { get; set; }
}