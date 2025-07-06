namespace Core.DTOs.Device;

public class DeviceAuditResponse {
    public required string   PreviousStatus { get; set; }
    public required string   NewStatus      { get; set; }
    public          string?  Reason         { get; set; }
    public          DateTime ChangedAt      { get; set; }
    public          string?  ChangedByUser  { get; set; }
}