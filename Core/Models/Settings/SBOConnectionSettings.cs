namespace Core.Models.Settings;

public class SBOConnectionSettings {
    public required string  Server            { get; set; }
    public          bool    TrustedConnection { get; set; }
    public          string? ServerUser        { get; set; }
    public          string? ServerPassword    { get; set; }
    public required string  User              { get; set; }
    public required string  Password          { get; set; }
}