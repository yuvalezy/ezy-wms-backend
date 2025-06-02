using Core.Models.Settings;

namespace Core.Interfaces;

public interface ISettings {
    public LoggingSettings           Logging           { get; set; }
    public string                    AllowedHosts      { get; set; }
    public ConnectionStringsSettings ConnectionStrings { get; set; }
    public JwtSettings               Jwt               { get; set; }
    public Options                   Options           { get; set; }
    public Filters                   Filters           { get; set; }
    public SessionManagementSettings SessionManagement { get; set; }
}