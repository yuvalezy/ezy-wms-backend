namespace Core.DTOs.License;

public class LicenseStatusResponse {
    public bool      IsValid              { get; set; }
    public string    AccountStatus        { get; set; } = string.Empty;
    public DateTime? ExpirationDate       { get; set; }
    public int       DaysUntilExpiration  { get; set; }
    public bool      IsInGracePeriod      { get; set; }
    public string?   WarningMessage       { get; set; }
    public bool      ShowWarning          { get; set; }
}