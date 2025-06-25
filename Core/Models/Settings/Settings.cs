using Core.Enums;
using Core.Interfaces;

namespace Core.Models.Settings;

public class Settings : ISettings {
    public LoggingSettings                   Logging           { get; set; }
    public string                            AllowedHosts      { get; set; }
    public ConnectionStringsSettings         ConnectionStrings { get; set; }
    public JwtSettings                       Jwt               { get; set; }
    public Options                           Options           { get; set; }
    public Filters                           Filters           { get; set; }
    public Dictionary<string, CustomField[]> CustomFields      { get; set; }
    public SessionManagementSettings         SessionManagement { get; set; }
    public SboSettings?                      SboSettings       { get; set; }
    public ExternalAdapterType               ExternalAdapter   { get; set; }
}