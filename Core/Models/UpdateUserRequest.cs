using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public class UpdateUserRequest : UserRequest {
    [MinLength(6)]
    public string? Password { get; set; }
}