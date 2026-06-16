using System.Text;
using System.Text.Json;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Services;

/// <summary>
/// Tests a SAP Business One Service Layer login with arbitrary (draft) settings,
/// independent of the shared <see cref="SboCompany"/> session. Used by the
/// "test connection" admin action before settings are saved.
/// </summary>
public sealed class SboConnectionTester(ILogger<SboConnectionTester> logger) : ISboConnectionTester {
    public async Task<SboConnectionResult> TestServiceLayerLoginAsync(
        SboSettings? settings, CancellationToken cancellationToken = default) {
        if (settings is null) {
            return new SboConnectionResult(false, "SAP Business One settings are not configured.");
        }
        if (string.IsNullOrWhiteSpace(settings.ServiceLayerUrl)) {
            return new SboConnectionResult(false, "Service Layer URL is required.");
        }
        if (string.IsNullOrWhiteSpace(settings.Database)) {
            return new SboConnectionResult(false, "Company database is required.");
        }
        if (string.IsNullOrWhiteSpace(settings.User)) {
            return new SboConnectionResult(false, "User is required.");
        }
        if (string.IsNullOrWhiteSpace(settings.Password)) {
            return new SboConnectionResult(false, "Password is required.");
        }

        using var handler = new HttpClientHandler {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        int timeout = settings.ServiceLayerTimeoutSeconds > 0 ? settings.ServiceLayerTimeoutSeconds : 60;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeout) };

        var payload = new { CompanyDB = settings.Database, UserName = settings.User, Password = settings.Password };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        string url = $"{settings.ServiceLayerUrl.TrimEnd('/')}/b1s/v2/Login";

        try {
            var response = await client.PostAsync(url, content, cancellationToken);
            if (response.IsSuccessStatusCode) {
                return new SboConnectionResult(true, "Connected to the SAP Service Layer successfully.");
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            string message = ExtractError(body) ?? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            return new SboConnectionResult(false, message);
        }
        catch (Exception ex) {
            // The catch only fires for transport-level failures (refused/timeout/DNS);
            // SAP credential/DB errors come back as a non-success HTTP status above.
            logger.LogWarning(ex, "SBO test connection failed for {Url}", url);
            return new SboConnectionResult(false,
                $"Could not reach the SAP Service Layer at {settings.ServiceLayerUrl}: {ex.Message}");
        }
    }

    private static string? ExtractError(string body) {
        try {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error)) {
                if (error.TryGetProperty("message", out var message)) {
                    if (message.ValueKind == JsonValueKind.String) {
                        return message.GetString();
                    }
                    if (message.ValueKind == JsonValueKind.Object && message.TryGetProperty("value", out var value)) {
                        return value.GetString();
                    }
                }
            }
        }
        catch {
            // fall through to generic message
        }
        return null;
    }
}
