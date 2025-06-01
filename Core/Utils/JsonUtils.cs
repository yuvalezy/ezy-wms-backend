using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Utils;

public static class JsonUtils {
    private static readonly JsonSerializerOptions Options = new() {
        Converters                  = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
    };

    public static T? Deserialize<T>(string jsonData) => JsonSerializer.Deserialize<T>(jsonData, Options);
}