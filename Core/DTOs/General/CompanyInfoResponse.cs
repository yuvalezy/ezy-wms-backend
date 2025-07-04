using Core.DTOs.License;

namespace Core.DTOs.General;

public class CompanyInfoResponse {
    public string?              CompanyName     { get; set; }
    public DateTime             ServerTime      { get; set; }
    public List<LicenseWarning> LicenseWarnings { get; set; } = [];
}