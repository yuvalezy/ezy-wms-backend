using Core.Interfaces;

namespace Core.Extensions;

public static class SettingsExtensions {
    public static int? GetInitialCountingBinEntry(this ISettings settings, string warehouse) {
        if (settings.Warehouses != null && settings.Warehouses.TryGetValue(warehouse, out var settingsWarehouse))
            return settingsWarehouse.InitialCountingBinEntry;

        throw new Exception($"Warehouse {warehouse} not found in settings");
    }

    public static int? GetStagingBinEntry(this ISettings settings, string warehouse) {
        if (settings.Warehouses != null && settings.Warehouses.TryGetValue(warehouse, out var settingsWarehouse))
            return settingsWarehouse.StagingBinEntry;

        throw new Exception($"Warehouse {warehouse} not found in settings");
    }

    public static int GetCancelPickingBinEntry(this ISettings settings, string warehouse) {
        if (settings.Warehouses != null && settings.Warehouses.TryGetValue(warehouse, out var settingsWarehouse))
            return settingsWarehouse.CancelPickingBinEntry;

        throw new Exception($"Warehouse {warehouse} not found in settings");
    }
}