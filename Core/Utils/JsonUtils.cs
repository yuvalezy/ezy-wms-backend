using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Models;

namespace Core.Utils;

public static class JsonUtils {
    public static readonly JsonSerializerOptions Options = new() {
        Converters                  = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
    };

    public static T? Deserialize<T>(string jsonData) => JsonSerializer.Deserialize<T>(jsonData, Options);

    public static string ToJson(this SessionInfo info) => JsonSerializer.Serialize(info, Options);
}