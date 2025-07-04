namespace Core.Models;

public class AccountValidationRequest {
    public List<string> ActiveDeviceUuids        { get; set; } = new List<string>();
    public DateTime     LastValidationTimestamp  { get; set; }
}