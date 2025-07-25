namespace Core.Models.Settings;

/// <summary>
/// Global settings for external commands system
/// </summary>
public class ExternalCommandsGlobalSettings {
    /// <summary>
    /// Maximum number of commands that can execute concurrently
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 5;
    
    /// <summary>
    /// Command execution timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;
    
    /// <summary>
    /// Retry policy configuration
    /// </summary>
    public CommandRetryPolicy RetryPolicy { get; set; } = new();
    
    /// <summary>
    /// File encoding for generated files
    /// </summary>
    public string FileEncoding { get; set; } = "UTF-8";
    
    /// <summary>
    /// XML-specific settings
    /// </summary>
    public XmlSettings XmlSettings { get; set; } = new();
    
    /// <summary>
    /// JSON-specific settings
    /// </summary>
    public JsonSettings JsonSettings { get; set; } = new();
}

/// <summary>
/// Retry policy configuration for command execution
/// </summary>
public class CommandRetryPolicy {
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Delay between retry attempts in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;
    
    /// <summary>
    /// Error types that should trigger retries
    /// </summary>
    public string[] RetryOnErrors { get; set; } = ["NetworkError", "TimeoutError"];
}

/// <summary>
/// XML formatting settings
/// </summary>
public class XmlSettings {
    /// <summary>
    /// Root element name for XML documents
    /// </summary>
    public string RootElementName { get; set; } = "Data";
    
    /// <summary>
    /// Whether to include XML declaration
    /// </summary>
    public bool IncludeXmlDeclaration { get; set; } = true;
    
    /// <summary>
    /// Whether to indent XML for readability
    /// </summary>
    public bool IndentXml { get; set; } = true;
}

/// <summary>
/// JSON formatting settings
/// </summary>
public class JsonSettings {
    /// <summary>
    /// Whether to indent JSON for readability
    /// </summary>
    public bool IndentJson { get; set; } = true;
    
    /// <summary>
    /// Whether to use camelCase for property names
    /// </summary>
    public bool CamelCasePropertyNames { get; set; } = false;
    
    /// <summary>
    /// Date format string for JSON serialization
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-MM-ddTHH:mm:ss";
}