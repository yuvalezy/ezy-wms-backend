namespace Core.Models.Settings;

public class LicensingSettings {
    public string  EncryptionKey         { get; set; } = string.Empty;
    public string  CloudEndpoint         { get; set; } = string.Empty;
    public string  BearerToken           { get; set; } = string.Empty;
    public int     CacheExpirationHours  { get; set; } = 24;
    public int     GracePeriodDays       { get; set; } = 7;
    public int     DemoExpirationDays    { get; set; } = 30;
    public int     WarningThresholdDays  { get; set; } = 3;
}