using Core.Models.Settings;

namespace Core.Interfaces;

public class ISettings {
    public LoggingSettings           Logging           { get; set; } = new();
    public string                    AllowedHosts      { get; set; }
    public ConnectionStringsSettings ConnectionStrings { get; set; }
    public JwtSettings               Jwt               { get; set; }
    public Options                   Options           { get; set; }
}