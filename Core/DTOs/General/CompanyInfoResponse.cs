using Core.DTOs.License;
using Core.Enums;

namespace Core.DTOs.General;

public class CompanyInfoResponse {
    public string?              CompanyName     { get; set; }
    public DateTime             ServerTime      { get; set; }
    public List<LicenseWarning> LicenseWarnings { get; set; } = [];
    public DeviceStatus?        DeviceStatus    { get; set; }
    public AccountState?        AccountStatus   { get; set; }
}