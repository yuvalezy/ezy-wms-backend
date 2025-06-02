using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public class UserRequest {
    [Required]
    [MaxLength(50)]
    public required string FullName { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? Position { get; set; }

    public bool SuperUser { get; set; }

    [Required]
    public ICollection<string> Warehouses { get; set; } = [];

    [MaxLength(50)]
    public string? ExternalId { get; set; }

    public Guid? AuthorizationGroupId { get; set; }
}