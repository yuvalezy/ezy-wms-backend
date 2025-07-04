namespace Core.DTOs.General;

public class LoginRequest {
    public required string  Password  { get; set; }
    public          string? Warehouse { get; set; }
    public          string? NewDeviceName { get; set; }
}