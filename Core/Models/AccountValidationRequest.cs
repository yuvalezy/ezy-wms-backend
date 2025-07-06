namespace Core.Models;

public class AccountValidationRequest {
    public List<string> ActiveDeviceUuids        { get; set; } = [];
    public DateTime     LastValidationTimestamp  { get; set; }
}