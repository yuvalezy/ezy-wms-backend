namespace Core.DTOs.License;

public class DeviceValidationResponse {
    public bool     IsValid             { get; set; }
    public string?  DeviceStatus        { get; set; }
    public DateTime ValidationTimestamp { get; set; }
}