using Core.Enums;

namespace Core.Models.Settings;

/// <summary>
/// Defines the destination configuration for external command file delivery
/// </summary>
public class CommandDestination {
    /// <summary>
    /// Type of destination
    /// </summary>
    public CommandDestinationType Type { get; set; }
    
    /// <summary>
    /// Path, URL, or host depending on destination type
    /// </summary>
    public required string Path { get; set; }
    
    /// <summary>
    /// Host for FTP/SFTP connections
    /// </summary>
    public string? Host { get; set; }
    
    /// <summary>
    /// Port for FTP/SFTP connections
    /// </summary>
    public int? Port { get; set; }
    
    /// <summary>
    /// Username for authentication
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// Password for authentication (should be encrypted)
    /// </summary>
    public string? Password { get; set; }
    
    /// <summary>
    /// Whether to use network impersonation for NetworkPath destinations
    /// </summary>
    public bool UseNetworkImpersonation { get; set; } = false;
    
    /// <summary>
    /// Use passive mode for FTP connections
    /// </summary>
    public bool UsePassiveMode { get; set; } = true;
    
    /// <summary>
    /// Use SSL/TLS for FTP connections
    /// </summary>
    public bool UseSsl { get; set; } = false;
    
    /// <summary>
    /// Private key file path for SFTP connections
    /// </summary>
    public string? PrivateKeyPath { get; set; }
    
    /// <summary>
    /// Private key passphrase for SFTP connections
    /// </summary>
    public string? PrivateKeyPassphrase { get; set; }
    
    /// <summary>
    /// Host fingerprint for SFTP connections (for security verification)
    /// </summary>
    public string? HostFingerprint { get; set; }
}