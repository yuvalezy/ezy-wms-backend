using Core.Enums;

namespace Core.DTOs.Alerts;

public class WmsAlertResponse {
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public WmsAlertType AlertType { get; set; }
    public WmsAlertObjectType ObjectType { get; set; }
    public Guid ObjectId { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
