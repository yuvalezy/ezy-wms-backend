using Microsoft.Extensions.Configuration;

namespace Infrastructure.Configuration;

/// <summary>
/// Configuration source backing <see cref="DatabaseConfigurationProvider"/>.
/// Keeps a reference to the built provider so the application can trigger a
/// reload after editing configuration.
/// </summary>
public sealed class DatabaseConfigurationSource(string? connectionString, string? encryptionKey)
    : IConfigurationSource {

    public DatabaseConfigurationProvider? Provider { get; private set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder) {
        Provider = new DatabaseConfigurationProvider(connectionString, new ConfigSecretProtector(encryptionKey));
        return Provider;
    }
}

public static class DatabaseConfigurationExtensions {
    /// <summary>
    /// Adds the database-backed configuration provider. Registered LAST so that,
    /// once the database holds configuration, it takes precedence over any leftover
    /// YAML files.
    /// </summary>
    public static IConfigurationBuilder AddDatabaseConfiguration(
        this IConfigurationBuilder builder, string? connectionString, string? encryptionKey) {
        return builder.Add(new DatabaseConfigurationSource(connectionString, encryptionKey));
    }
}
