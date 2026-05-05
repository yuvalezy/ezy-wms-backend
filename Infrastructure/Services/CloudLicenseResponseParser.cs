using System.Text.Json;
using Core.Models;
using Core.Utils;

namespace Infrastructure.Services;

internal static class CloudLicenseResponseParser {
    public static AccountValidationResponse ParseAccountValidationResponse(string json) {
        var result = ParseWrappedResponse<AccountValidationResponse>(json, response =>
            new AccountValidationResponse {
                Success = response.Success,
                Message = response.Message,
                LicenseData = response.Data?.LicenseData,
                DevicesToDeactivate = response.Data?.DevicesToDeactivate,
                ServerTimestamp = UseDataTimestamp(response.Data?.ServerTimestamp, response.Timestamp)
            });

        if (result.ServerTimestamp == default) {
            result.ServerTimestamp = ReadRootTimestamp(json);
        }

        return result;
    }

    public static CloudLicenseResponse ParseDeviceEventResponse(string json) {
        var result = ParseWrappedResponse<CloudLicenseResponse>(json, response =>
            new CloudLicenseResponse {
                Success = response.Success,
                Message = response.Data?.Message ?? response.Message,
                LicenseData = response.Data?.LicenseData,
                ServerTimestamp = UseDataTimestamp(response.Data?.ServerTimestamp, response.Timestamp)
            });

        if (result.ServerTimestamp == default) {
            result.ServerTimestamp = ReadRootTimestamp(json);
        }

        return result;
    }

    private static T ParseWrappedResponse<T>(string json, Func<ApiEnvelope<T>, T> unwrap) where T : class, new() {
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object) {
            var envelope = JsonUtils.Deserialize<ApiEnvelope<T>>(json);
            if (envelope != null) {
                return unwrap(envelope);
            }
        }

        return JsonUtils.Deserialize<T>(json) ?? new T();
    }

    private static DateTime UseDataTimestamp(DateTime? dataTimestamp, DateTime envelopeTimestamp) {
        return dataTimestamp is { } timestamp && timestamp != default ? timestamp : envelopeTimestamp;
    }

    private static DateTime ReadRootTimestamp(string json) {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("timestamp", out var timestampElement) &&
               timestampElement.TryGetDateTime(out var timestamp)
            ? timestamp
            : default;
    }

    private sealed class ApiEnvelope<T> {
        public bool      Success   { get; set; }
        public T?        Data      { get; set; }
        public ApiError? Error     { get; set; }
        public DateTime  Timestamp { get; set; }

        public string Message =>
            Error?.Message ?? (Success ? "Request completed successfully" : "Request failed");
    }

    private sealed class ApiError {
        public string Message { get; set; } = string.Empty;
    }
}
