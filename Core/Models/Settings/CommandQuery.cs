using Core.Enums;

namespace Core.Models.Settings;

/// <summary>
/// Defines a query to be executed as part of an external command
/// </summary>
public class CommandQuery {
    /// <summary>
    /// Name of the query for identification
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// SQL query to execute
    /// </summary>
    public required string Query { get; set; }
    
    /// <summary>
    /// Expected result type (Single row or Multiple rows)
    /// </summary>
    public CommandQueryResultType ResultType { get; set; } = CommandQueryResultType.Single;
    
    /// <summary>
    /// Indicates if this query supports batch execution with multiple IDs
    /// </summary>
    public bool IsBatchQuery { get; set; } = false;
}