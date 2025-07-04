namespace Core.Models;

public class CloudLicenseRequest {
    public string                      DeviceUuid     { get; set; } = string.Empty;
    public string                      Event          { get; set; } = string.Empty; // register, activate, deactivate, disable
    public string                      DeviceName     { get; set; } = string.Empty;
    public DateTime                    Timestamp      { get; set; }
    public Dictionary<string, object>  AdditionalData { get; set; } = new Dictionary<string, object>();
}