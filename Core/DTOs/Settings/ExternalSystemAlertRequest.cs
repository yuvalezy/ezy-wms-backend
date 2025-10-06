using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Settings;

public class ExternalSystemAlertRequest {
    [Required]
    public AlertableObjectType ObjectType { get; set; }

    [Required]
    [MaxLength(50)]
    public required string ExternalUserId { get; set; }

    public bool Enabled { get; set; } = true;
}

public class ExternalSystemAlertUpdateRequest {
    [Required]
    public bool Enabled { get; set; }
}
