namespace Core.DTOs;

public class LoginRequest {
    public required string  Password  { get; set; }
    public          string? Warehouse { get; set; }
}