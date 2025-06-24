using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Settings;

public class UpdateUserRequest : UserRequest {
    [MinLength(6)]
    public string? Password { get; set; }
}