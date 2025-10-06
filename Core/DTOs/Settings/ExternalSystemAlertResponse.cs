using Core.Enums;

namespace Core.DTOs.Settings;

public class ExternalSystemAlertResponse {
    public Guid Id { get; set; }
    public AlertableObjectType ObjectType { get; set; }
    public required string ExternalUserId { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
