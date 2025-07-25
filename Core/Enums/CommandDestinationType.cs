namespace Core.Enums;

/// <summary>
/// Defines the type of destination for external command file delivery
/// </summary>
public enum CommandDestinationType {
    /// <summary>
    /// Local file system path
    /// </summary>
    LocalPath = 0,
    
    /// <summary>
    /// Network share path (UNC path)
    /// </summary>
    NetworkPath = 1,
    
    /// <summary>
    /// FTP server
    /// </summary>
    FTP = 2,
    
    /// <summary>
    /// SFTP (SSH File Transfer Protocol) server
    /// </summary>
    SFTP = 3
}