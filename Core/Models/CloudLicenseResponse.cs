namespace Core.Models;

public class CloudLicenseResponse {
    public bool         Success         { get; set; }
    public string       Message         { get; set; } = string.Empty;
    public LicenseData? LicenseData     { get; set; }
    public DateTime     ServerTimestamp { get; set; }
}