using System;
using Core.Interfaces;

namespace Service.Extensions;

public static class SettingsLoggingExtensions {
    /// <summary>
    /// Logs configurations that will be moved to YAML files during migration
    /// </summary>
    /// <param name="settings">The settings object to log</param>
    public static void LogYamlMigrationCandidates(this ISettings settings) {
        Console.WriteLine("📋 Configurations loaded (future YAML migration candidates):");
        Console.WriteLine($"  📁 CustomFields: {settings.CustomFields?.Count ?? 0} items");
        Console.WriteLine($"  ⚡ ExternalCommands: {settings.ExternalCommands.Commands.Length} commands");
        Console.WriteLine($"  📦 Package.MetadataDefinition: {settings.Package.MetadataDefinition.Length} fields");
        Console.WriteLine($"  🏷️  Item.MetadataDefinition: {settings.Item.MetadataDefinition.Length} fields");
        Console.WriteLine($"  🏪 Warehouses: {settings.Warehouses?.Count ?? 0} configured");
        Console.WriteLine($"  🔍 Filters: Vendors={settings.Filters.Vendors != null}, PickPackOnly={settings.Filters.PickPackOnly != null}");
        Console.WriteLine(
            $"  ⚙️  Options: WhsCodeBinSuffix={settings.Options.WhsCodeBinSuffix}, EnablePackages={settings.Options.EnablePackages}, EnablePickingCheck={settings.Options.EnablePickingCheck}");

        Console.WriteLine($"  🔄 BackgroundServices: PickListSync={settings.BackgroundServices.PickListSync.Enabled}, CloudSync={settings.BackgroundServices.CloudSync.Enabled}");
        Console.WriteLine($"  🔧 PickingPostProcessing: {settings.PickingPostProcessing.Processors.Count} processors");
        Console.WriteLine();
    }
}