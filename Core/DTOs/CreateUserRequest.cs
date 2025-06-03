using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CreateUserRequest : UserRequest {
    [Required]
    [MinLength(6)]
    public required string Password { get; set; }
}