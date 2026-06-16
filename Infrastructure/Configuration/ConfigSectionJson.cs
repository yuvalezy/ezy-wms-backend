using System.Text;
using System.Text.Json.Nodes;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Configuration;

/// <summary>
/// Converts between an <see cref="IConfigurationSection"/> subtree and
/// binder-compatible JSON, and back into flat configuration key/value pairs.
///
/// Operates entirely at the configuration-key level (never via the bound POCO),
/// so <c>[JsonIgnore]</c> SQL fields (CustomField.Query/GroupBy, etc.) are
/// preserved. Secret leaves are encrypted on the way to JSON and decrypted on the
/// way back, keyed by the marker prefix.
/// </summary>
public static class ConfigSectionJson {
    /// <summary>Serializes a configuration section's subtree to JSON, encrypting secret leaves.</summary>
    public static string SectionToJson(IConfigurationSection section, ConfigSecretProtector protector) {
        JsonNode? node = BuildNode(section, section.Key, protector);
        return node?.ToJsonString() ?? "null";
    }

    /// <summary>Flattens stored section JSON into configuration key/values, decrypting secret leaves.</summary>
    public static IEnumerable<KeyValuePair<string, string?>> Flatten(
        string section, string json, ConfigSecretProtector protector) {
        JsonNode? inner = json is "null" or "" ? null : JsonNode.Parse(json);
        var wrapper = new JsonObject { [section] = inner };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(wrapper.ToJsonString()));
        IConfigurationRoot cfg = new ConfigurationBuilder().AddJsonStream(stream).Build();

        foreach (var kv in cfg.AsEnumerable()) {
            if (kv.Value is null) {
                continue;
            }
            string value = ConfigSectionCatalog.IsEncrypted(kv.Value) ? protector.Unprotect(kv.Value) : kv.Value;
            yield return new KeyValuePair<string, string?>(kv.Key, value);
        }
    }

    private static JsonNode? BuildNode(IConfigurationSection section, string fullKey, ConfigSecretProtector protector) {
        var children = section.GetChildren().ToList();

        if (children.Count == 0) {
            string? value = section.Value;
            if (value is null) {
                return null;
            }
            if (value.Length > 0 && ConfigSectionCatalog.IsSecretKey(fullKey)) {
                value = protector.Protect(value);
            }
            return JsonValue.Create(value);
        }

        if (IsSequentialArray(children, out var ordered)) {
            var arr = new JsonArray();
            foreach (var child in ordered) {
                arr.Add(BuildNode(child, $"{fullKey}:{child.Key}", protector));
            }
            return arr;
        }

        var obj = new JsonObject();
        foreach (var child in children) {
            obj[child.Key] = BuildNode(child, $"{fullKey}:{child.Key}", protector);
        }
        return obj;
    }

    private static bool IsSequentialArray(List<IConfigurationSection> children, out List<IConfigurationSection> ordered) {
        ordered = children;
        var indices = new List<int>(children.Count);
        foreach (var child in children) {
            if (!int.TryParse(child.Key, out int i)) {
                return false;
            }
            indices.Add(i);
        }

        indices.Sort();
        for (int i = 0; i < indices.Count; i++) {
            if (indices[i] != i) {
                return false;
            }
        }

        ordered = children.OrderBy(c => int.Parse(c.Key)).ToList();
        return true;
    }
}
