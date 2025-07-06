namespace Core.Models;

public class AccountValidationResponse {
    public bool         Success             { get; set; }
    public string       Message             { get; set; } = string.Empty;
    public LicenseData? LicenseData         { get; set; }
    public List<string> DevicesToDeactivate { get; set; } = [];
    public DateTime     ServerTimestamp      { get; set; }
}