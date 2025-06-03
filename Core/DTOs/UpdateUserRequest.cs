using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateUserRequest : UserRequest {
    [MinLength(6)]
    public string? Password { get; set; }
}