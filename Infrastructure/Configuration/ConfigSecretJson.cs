using System.Text.Json.Nodes;
using Core.Configuration;

namespace Infrastructure.Configuration;

/// <summary>
/// JSON tree helpers for handling secret leaves and restricted (code-loading)
/// fields in a configuration section payload. Secret leaves are identified by
/// property name (see <see cref="ConfigSectionCatalog"/>).
/// </summary>
public static class ConfigSecretJson {
    /// <summary>Replaces non-empty secret leaf values with the mask sentinel (for reads/exports).</summary>
    public static string Mask(string json) {
        JsonNode? node = TryParse(json);
        if (node is null) {
            return json;
        }
        Transform(node, (key, value) =>
            ConfigSectionCatalog.IsSecretKey(key) && value.Length > 0 ? ConfigSectionCatalog.SecretMask : null);
        return node.ToJsonString();
    }

    /// <summary>Encrypts plaintext secret leaves in place (idempotent for already-encrypted values).</summary>
    public static string Encrypt(string json, ConfigSecretProtector protector) {
        JsonNode? node = TryParse(json);
        if (node is null) {
            return json;
        }
        Transform(node, (key, value) =>
            ConfigSectionCatalog.IsSecretKey(key) && value.Length > 0 ? protector.Protect(value) : null);
        return node.ToJsonString();
    }

    /// <summary>
    /// Where an incoming secret leaf equals the mask sentinel, restores the value
    /// from the existing stored payload (kept encrypted). Returns merged JSON.
    /// </summary>
    public static string MergeMaskedSecrets(string incomingJson, string? existingJson) {
        JsonNode? incoming = TryParse(incomingJson);
        JsonNode? existing = existingJson is null ? null : TryParse(existingJson);
        if (incoming is null) {
            return incomingJson;
        }
        MergeMasked(incoming, existing);
        return incoming.ToJsonString();
    }

    /// <summary>True if the payload contains any non-empty secret leaf.</summary>
    public static bool HasSecret(string json) {
        JsonNode? node = TryParse(json);
        if (node is null) {
            return false;
        }
        bool found = false;
        Transform(node, (key, value) => {
            if (ConfigSectionCatalog.IsSecretKey(key) && value.Length > 0) {
                found = true;
            }
            return null;
        });
        return found;
    }

    /// <summary>Collects (assembly, type) pairs declared by restricted code-loading objects.</summary>
    public static List<(string Assembly, string Type)> CollectAssemblyTypePairs(string json) {
        var pairs = new List<(string, string)>();
        JsonNode? node = TryParse(json);
        if (node is not null) {
            CollectPairs(node, pairs);
        }
        return pairs;
    }

    private static JsonNode? TryParse(string json) {
        if (json is "null" or "") {
            return null;
        }
        try {
            return JsonNode.Parse(json);
        }
        catch {
            return null;
        }
    }

    /// <summary>Walks the tree; for each string leaf, replaces its value when <paramref name="leaf"/> returns non-null.</summary>
    private static void Transform(JsonNode node, Func<string, string, string?> leaf) {
        switch (node) {
            case JsonObject obj:
                foreach (var prop in obj.ToList()) {
                    if (prop.Value is JsonObject or JsonArray) {
                        Transform(prop.Value!, leaf);
                    }
                    else if (prop.Value is JsonValue jv && jv.TryGetValue<string>(out var s)) {
                        string? replacement = leaf(prop.Key, s);
                        if (replacement is not null) {
                            obj[prop.Key] = replacement;
                        }
                    }
                }
                break;
            case JsonArray arr:
                foreach (var child in arr) {
                    if (child is JsonObject or JsonArray) {
                        Transform(child, leaf);
                    }
                }
                break;
        }
    }

    private static void MergeMasked(JsonNode incoming, JsonNode? existing) {
        switch (incoming) {
            case JsonObject io:
                var eo = existing as JsonObject;
                foreach (var prop in io.ToList()) {
                    JsonNode? ev = eo is not null && eo.TryGetPropertyValue(prop.Key, out var e) ? e : null;
                    if (prop.Value is JsonObject or JsonArray) {
                        MergeMasked(prop.Value!, ev);
                    }
                    else if (ConfigSectionCatalog.IsSecretKey(prop.Key)
                             && prop.Value is JsonValue jv && jv.TryGetValue<string>(out var s)
                             && s == ConfigSectionCatalog.SecretMask) {
                        if (ev is not null) {
                            io[prop.Key] = ev.DeepClone();
                        }
                        else {
                            io.Remove(prop.Key);
                        }
                    }
                }
                break;
            case JsonArray ia:
                var ea = existing as JsonArray;
                for (int i = 0; i < ia.Count; i++) {
                    JsonNode? ev = ea is not null && i < ea.Count ? ea[i] : null;
                    if (ia[i] is JsonObject or JsonArray) {
                        MergeMasked(ia[i]!, ev);
                    }
                }
                break;
        }
    }

    private static void CollectPairs(JsonNode node, List<(string, string)> pairs) {
        switch (node) {
            case JsonObject obj:
                string? assembly = obj["Assembly"]?.GetValue<string>();
                string? type     = obj["TypeName"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(assembly) || !string.IsNullOrWhiteSpace(type)) {
                    pairs.Add((assembly ?? "", type ?? ""));
                }
                foreach (var prop in obj) {
                    if (prop.Value is JsonObject or JsonArray) {
                        CollectPairs(prop.Value!, pairs);
                    }
                }
                break;
            case JsonArray arr:
                foreach (var child in arr) {
                    if (child is JsonObject or JsonArray) {
                        CollectPairs(child, pairs);
                    }
                }
                break;
        }
    }
}
