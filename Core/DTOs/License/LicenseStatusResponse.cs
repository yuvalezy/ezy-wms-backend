using Core.Entities;

namespace Core.DTOs.License;

public class LicenseStatusResponse {
    public          bool            IsValid             { get; set; }
    public required AccountStatus   AccountStatus       { get; set; }
    public          DateTime?       ExpirationDate      { get; set; }
    public          int             DaysUntilExpiration { get; set; }
    public          bool            IsInGracePeriod     { get; set; }
    public          LicenseWarning? Warning             { get; set; }
    public          bool            ShowWarning         { get; set; }
}