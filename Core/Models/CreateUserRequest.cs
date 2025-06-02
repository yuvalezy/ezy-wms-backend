using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public class CreateUserRequest : UserRequest {
    [Required]
    [MinLength(6)]
    public required string Password { get; set; }
}