namespace Core.Entities;

public class WmsSession {
    public string    Id          { get; set; } = string.Empty;
    public string    SessionData { get; set; } = string.Empty;
    public DateTime? ExpiresAt   { get; set; }
    public DateTime  CreatedAt   { get; set; }
    public DateTime  UpdatedAt   { get; set; }
}
