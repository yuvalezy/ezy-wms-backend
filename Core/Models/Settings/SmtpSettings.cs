namespace Core.Models.Settings;

public class SmtpSettings {
    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "EzyWMS";
    public int TimeoutSeconds { get; set; } = 30;
    public string TimeZoneId { get; set; } = "America/Panama";
}
