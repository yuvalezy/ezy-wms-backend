using Core.Enums;
using Core.Models.Settings;

namespace Core.Interfaces;

public interface ISettings {
    public LoggingSettings Logging { get; set; }
    public string AllowedHosts { get; set; }
    public ConnectionStringsSettings ConnectionStrings { get; set; }
    public JwtSettings Jwt { get; set; }
    public Options Options { get; set; }
    public Filters Filters { get; set; }
    public Dictionary<string, CustomField[]>? CustomFields { get; set; }
    public Dictionary<string, WarehouseSettings>? Warehouses { get; set; }
    public SessionManagementSettings SessionManagement { get; set; }
    public SboSettings? SboSettings { get; set; }
    public ExternalAdapterType ExternalAdapter { get; set; }
    public PackageSettings Package { get; set; }
    public ItemSettings Item { get; set; }
    public LicensingSettings Licensing { get; set; }
    public BackgroundServicesSettings BackgroundServices { get; set; }
    public ExternalCommandsSettings ExternalCommands { get; set; }
}