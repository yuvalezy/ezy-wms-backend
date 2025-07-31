namespace Core.Models.Settings;

public class SboSettings {
    public required string  Server          { get; set; }
    public          string? ServiceLayerUrl { get; set; }
    public          int     ServerType        { get; set; }
    public          bool    TrustedConnection { get; set; }
    public          string? ServerUser        { get; set; } 
    public          string? ServerPassword    { get; set; }
    public required string  Database          { get; set; }
    public          string? User              { get; set; }
    public          string? Password          { get; set; }
}