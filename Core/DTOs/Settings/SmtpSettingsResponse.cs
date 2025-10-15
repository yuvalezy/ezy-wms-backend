namespace Core.DTOs.Settings;

public class SmtpSettingsResponse {
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public string? Username { get; set; }
    public bool HasPassword { get; set; }  // Don't expose actual password
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
}
