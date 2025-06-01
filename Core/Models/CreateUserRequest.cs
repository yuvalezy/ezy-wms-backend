using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public class CreateUserRequest
{
    [Required]
    [MaxLength(50)]
    public required string FullName { get; set; }
    
    [Required]
    [MinLength(6)]
    public required string Password { get; set; }
    
    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    
    [MaxLength(100)]
    public string? Position { get; set; }
    
    public bool SuperUser { get; set; }
    
    public Guid? AuthorizationGroupId { get; set; }
}