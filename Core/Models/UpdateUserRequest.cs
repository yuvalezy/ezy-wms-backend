using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public class UpdateUserRequest
{
    [MaxLength(50)]
    public string? FullName { get; set; }
    
    [MinLength(6)]
    public string? Password { get; set; }
    
    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    
    [MaxLength(100)]
    public string? Position { get; set; }
    
    public bool? SuperUser { get; set; }
    
    public Guid? AuthorizationGroupId { get; set; }
}