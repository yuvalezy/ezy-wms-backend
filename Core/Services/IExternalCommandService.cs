using Core.Enums;
using Core.Models.Settings;

namespace Core.Services;

/// <summary>
/// Service for executing external commands
/// </summary>
public interface IExternalCommandService {
    /// <summary>
    /// Executes all commands that match the specified trigger type and object type
    /// </summary>
    /// <param name="triggerType">The trigger type to match</param>
    /// <param name="objectType">The object type to match</param>
    /// <param name="objectId">The ID of the object that triggered the command</param>
    /// <param name="parameters">Additional parameters for command execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the execution operation</returns>
    Task ExecuteCommandsAsync(CommandTriggerType triggerType, ObjectType objectType, Guid objectId, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a specific command by ID
    /// </summary>
    /// <param name="commandId">The command ID to execute</param>
    /// <param name="objectId">The ID of the object</param>
    /// <param name="parameters">Additional parameters for command execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the execution operation</returns>
    Task ExecuteCommandAsync(string commandId, Guid objectId, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a batch command for multiple objects
    /// </summary>
    /// <param name="commandId">The command ID to execute</param>
    /// <param name="objectIds">The IDs of the objects</param>
    /// <param name="parameters">Additional parameters for command execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the execution operation</returns>
    Task ExecuteBatchCommandAsync(string commandId, Guid[] objectIds, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all commands that can be manually triggered for the specified object type
    /// </summary>
    /// <param name="objectType">The object type</param>
    /// <param name="screenName">The screen name where the command will be triggered</param>
    /// <returns>List of manual commands</returns>
    Task<IEnumerable<ExternalCommand>> GetManualCommandsAsync(ObjectType objectType, string screenName);
    
    /// <summary>
    /// Validates a command configuration
    /// </summary>
    /// <param name="command">The command to validate</param>
    /// <returns>List of validation errors (empty if valid)</returns>
    Task<IEnumerable<string>> ValidateCommandAsync(ExternalCommand command);
}