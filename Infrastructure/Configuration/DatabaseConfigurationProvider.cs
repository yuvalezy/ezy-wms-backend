using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Configuration;

/// <summary>
/// Serves configuration sections from the <c>SystemConfiguration</c> table into
/// <see cref="IConfiguration"/> using the same key semantics as a JSON source, so
/// <c>builder.Configuration.Bind(settings)</c> works unchanged.
///
/// Runs before the DI container exists, so it reads via raw ADO.NET. Tolerant of a
/// missing/empty table (fresh database) — it yields no keys and never throws, so
/// the YAML providers and/or the seed step can supply configuration on first boot.
/// </summary>
public sealed class DatabaseConfigurationProvider(string? connectionString, ConfigSecretProtector protector)
    : ConfigurationProvider {

    public override void Load() {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(connectionString)) {
            Data = data;
            return;
        }

        try {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT [Section], [Json] FROM [SystemConfiguration]";

            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                string section = reader.GetString(0);
                string json    = reader.GetString(1);

                foreach (var kv in ConfigSectionJson.Flatten(section, json, protector)) {
                    data[kv.Key] = kv.Value;
                }
            }
        }
        catch (SqlException ex) {
            // 208 = invalid object name (table not created yet on a fresh database).
            // Any read failure -> behave as "no DB config" so first boot can proceed.
            Console.WriteLine($"[config] Database configuration not loaded ({ex.Number}): {ex.Message}");
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            return;
        }
        catch (Exception ex) {
            Console.WriteLine($"[config] Database configuration not loaded: {ex.Message}");
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        Data = data;
    }

    /// <summary>Re-reads the table and notifies the configuration system (hot-reload).</summary>
    public void Reload() {
        Load();
        OnReload();
    }
}
