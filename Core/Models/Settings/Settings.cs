using Core.Enums;
using Core.Interfaces;

namespace Core.Models.Settings;

public class Settings : ISettings {
    public Options Options { get; set; } = new();
    public Filters Filters { get; set; } = new();
    public Dictionary<string, CustomField[]>? CustomFields { get; set; }
    public Dictionary<string, WarehouseSettings>? Warehouses { get; set; }
    public SessionManagementSettings SessionManagement { get; set; } = new();
    public SboSettings? SboSettings { get; set; }
    public ExternalAdapterType ExternalAdapter { get; set; } = new();
    public PackageSettings Package { get; set; } = new();
    public ItemSettings Item { get; set; } = new();
    public LicensingSettings Licensing { get; set; } = new();
    public BackgroundServicesSettings BackgroundServices { get; set; } = new();
    public ExternalCommandsSettings ExternalCommands { get; set; } = new();
    public PickingPostProcessingSettings PickingPostProcessing { get; set; } = new();
}