namespace Core.Enums;

/// <summary>
/// Defines the expected result type of a command query
/// </summary>
public enum CommandQueryResultType {
    /// <summary>
    /// Query returns a single row
    /// </summary>
    Single = 0,
    
    /// <summary>
    /// Query returns multiple rows
    /// </summary>
    Multiple = 1
}