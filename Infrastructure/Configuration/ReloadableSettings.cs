using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Configuration;

/// <summary>
/// <see cref="ISettings"/> implementation that always reads the current bound
/// <see cref="Settings"/> from <see cref="IOptionsMonitor{TOptions}"/>. Registered
/// as the singleton ISettings so runtime consumers observe configuration changes
/// after a reload, without a restart, for live-reloadable sections.
///
/// Getters and setters delegate to <c>CurrentValue</c> — existing code that
/// mutates e.g. <c>settings.Options.ScannerMode</c> per-request keeps working.
/// </summary>
public sealed class ReloadableSettings(IOptionsMonitor<Settings> monitor) : ISettings {
    private Settings Current => monitor.CurrentValue;

    public Core.Models.Settings.Options Options {
        get => Current.Options;
        set => Current.Options = value;
    }

    public Filters Filters {
        get => Current.Filters;
        set => Current.Filters = value;
    }

    public Dictionary<string, CustomField[]>? CustomFields {
        get => Current.CustomFields;
        set => Current.CustomFields = value;
    }

    public Dictionary<string, WarehouseSettings>? Warehouses {
        get => Current.Warehouses;
        set => Current.Warehouses = value;
    }

    public SessionManagementSettings SessionManagement {
        get => Current.SessionManagement;
        set => Current.SessionManagement = value;
    }

    public PresenceTrackingSettings PresenceTracking {
        get => Current.PresenceTracking;
        set => Current.PresenceTracking = value;
    }

    public SboSettings? SboSettings {
        get => Current.SboSettings;
        set => Current.SboSettings = value;
    }

    public ExternalAdapterType ExternalAdapter {
        get => Current.ExternalAdapter;
        set => Current.ExternalAdapter = value;
    }

    public MetaDataDefinitions Item {
        get => Current.Item;
        set => Current.Item = value;
    }

    public LicensingSettings Licensing {
        get => Current.Licensing;
        set => Current.Licensing = value;
    }

    public BackgroundServicesSettings BackgroundServices {
        get => Current.BackgroundServices;
        set => Current.BackgroundServices = value;
    }

    public ExternalCommandsSettings ExternalCommands {
        get => Current.ExternalCommands;
        set => Current.ExternalCommands = value;
    }

    public List<PickingPostProcessorSettings> PickingPostProcessingProcessors {
        get => Current.PickingPostProcessingProcessors;
        set => Current.PickingPostProcessingProcessors = value;
    }

    public SmtpSettings Smtp {
        get => Current.Smtp;
        set => Current.Smtp = value;
    }
}
