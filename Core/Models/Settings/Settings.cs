using Core.Interfaces;

namespace Core.Models.Settings;

public class Settings : ISettings {
    public LoggingSettings           Logging           { get; set; } = new();
    public string                    AllowedHosts      { get; set; }
    public ConnectionStringsSettings ConnectionStrings { get; set; }
    public JwtSettings               Jwt               { get; set; }
    public Options                   Options           { get; set; }
    public SBOConnectionSettings     SBOConnection     { get; set; }
}