using Core.Interfaces;

namespace Core.Extensions;

public static class SettingsExtensions {
    public static int? GetInitialCountingBinEntry(this ISettings settings, string warehouse) {
        if (settings.Warehouses != null)
            return settings.Warehouses[warehouse].InitialCountingBinEntry;

        throw new Exception($"Warehouse {warehouse} not found in settings");
    }

    public static int? GetStagingBinEntry(this ISettings settings, string warehouse) {
        if (settings.Warehouses != null)
            return settings.Warehouses[warehouse].StagingBinEntry;

        throw new Exception($"Warehouse {warehouse} not found in settings");
    }

    public static int GetCancelPickingBinEntry(this ISettings settings, string warehouse) {
        if (settings.Warehouses != null)
            return settings.Warehouses[warehouse].CancelPickingBinEntry;

        throw new Exception($"Warehouse {warehouse} not found in settings");
    }
}