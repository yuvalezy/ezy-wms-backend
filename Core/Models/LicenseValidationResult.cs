using Core.DTOs.General;
using Core.DTOs.License;
using Core.Enums;

namespace Core.Models;

public class LicenseValidationResult {
    public bool            IsValid             { get; set; }
    public bool            IsInGracePeriod     { get; set; }
    public bool            ShowWarning         { get; set; }
    public LicenseWarning? Warning      { get; set; }
    public AccountState    AccountStatus       { get; set; }
    public DateTime?       ExpirationDate      { get; set; }
    public int             DaysUntilExpiration { get; set; }
}