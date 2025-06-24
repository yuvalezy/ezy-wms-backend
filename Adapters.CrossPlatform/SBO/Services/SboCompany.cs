using System.Text;
using System.Text.Json;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Services;

public class SboCompany(ISettings settings, ILogger<SboCompany> logger) {
    private readonly string url      = settings.SboSettings?.ServiceLayerUrl ?? throw new InvalidOperationException("SBO service layer URL is not configured.");
    private readonly string user     = settings.SboSettings.User ?? throw new InvalidOperationException("SBO user is not configured.");
    private readonly string password = settings.SboSettings.Password ?? throw new InvalidOperationException("SBO password is not configured.");
    private readonly string database = settings.SboSettings.Database ?? throw new InvalidOperationException("SBO database is not configured.");

    private readonly HttpClient    httpClient          = CreateHttpClient();
    private readonly SemaphoreSlim connectionSemaphore = new(1, 1);

    private static HttpClient CreateHttpClient() {
        var handler = new HttpClientHandler() {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        return new HttpClient(handler);
    }

    private DateTime sessionExpiry = DateTime.MinValue;

    public string? SessionId { get; private set; }

    public async Task<bool> ConnectCompany() {
        if (IsConnected()) {
            return true;
        }
        await connectionSemaphore.WaitAsync();
        try {
            if (IsConnected()) {
                return true;
            }

            logger.LogInformation("Connecting to Service Layer at {Url}", url);

            var loginData = new {
                CompanyDB = database,
                UserName  = user,
                Password  = password
            };

            string json    = JsonSerializer.Serialize(loginData);
            var    content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{url}/b1s/v2/Login", content);

            if (response.IsSuccessStatusCode) {
                string responseContent = await response.Content.ReadAsStringAsync();

                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);

                if (loginResponse != null) {
                    SessionId     = loginResponse.SessionId;
                    sessionExpiry = DateTime.UtcNow.AddMinutes(loginResponse.SessionTimeout);
                    logger.LogInformation("Successfully connected to Service Layer. Session expires at {Expiry}", sessionExpiry);
                    return true;
                }
            }

            logger.LogError("Failed to connect to Service Layer. Status: {StatusCode}", response.StatusCode);
            return false;
        }
        finally {
            connectionSemaphore.Release();
        }
    }

    private bool IsConnected() {
        return !string.IsNullOrEmpty(SessionId) && DateTime.UtcNow < sessionExpiry;
    }

    public async Task<T?> GetAsync<T>(string endpoint) {
        await ConnectCompany();

        string fullUrl = $"{url}/b1s/v2/{endpoint}";

        var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
            string content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<T>(content, options);
        }

        logger.LogWarning("GET failed with status {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
        return default;
    }

    public async Task<(bool success, string? errorMessage)> PatchAsync<T>(string endpoint, T data) {
        return await ExecuteHttpRequestAsync(HttpMethod.Patch, endpoint, data);
    }

    public async Task<(bool success, string? errorMessage)> PutAsync<T>(string endpoint, T data) {
        return await ExecuteHttpRequestAsync(HttpMethod.Put, endpoint, data);
    }

    public async Task<bool> DeleteAsync(string endpoint) {
        await ConnectCompany();

        string fullUrl = $"{url}/b1s/v2/{endpoint}";

        var request = new HttpRequestMessage(HttpMethod.Delete, fullUrl);
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
            logger.LogInformation("DELETE successful for endpoint {Endpoint}", endpoint);
        }
        else {
            logger.LogWarning("DELETE failed for {Endpoint} with status {StatusCode}: {ReasonPhrase}", endpoint, response.StatusCode, response.ReasonPhrase);
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<(bool success, string? errorMessage, T? result)> PostAsync<T>(string endpoint, object data) {
        return await ExecuteHttpRequestWithResponseAsync<T>(HttpMethod.Post, endpoint, data);
    }

    public async Task<(bool success, string? errorMessage)> PostAsync(string endpoint, object data) {
        return await ExecuteHttpRequestAsync(HttpMethod.Post, endpoint, data);
    }

    private async Task<(bool success, string? errorMessage)> ExecuteHttpRequestAsync(HttpMethod httpMethod, string endpoint, object data) {
        await ConnectCompany();

        string json    = JsonSerializer.Serialize(data);
        var    content = new StringContent(json, Encoding.UTF8, "application/json");

        string fullUrl = $"{url}/b1s/v2/{endpoint}";
        string methodName = httpMethod.Method;

        var request = new HttpRequestMessage(httpMethod, fullUrl);
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");
        request.Content = content;

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
            if (httpMethod == HttpMethod.Post) {
                string responseContent = await response.Content.ReadAsStringAsync();
            }
            logger.LogInformation("{Method} successful for endpoint {Endpoint}", methodName, endpoint);
            return (true, null);
        }

        return await HandleErrorResponse(response, methodName, endpoint);
    }

    private async Task<(bool success, string? errorMessage, T? result)> ExecuteHttpRequestWithResponseAsync<T>(HttpMethod httpMethod, string endpoint, object data) {
        await ConnectCompany();

        string json    = JsonSerializer.Serialize(data);
        var    content = new StringContent(json, Encoding.UTF8, "application/json");

        string fullUrl = $"{url}/b1s/v2/{endpoint}";
        string methodName = httpMethod.Method;

        var request = new HttpRequestMessage(httpMethod, fullUrl);
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");
        request.Content = content;

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
            string responseContent = await response.Content.ReadAsStringAsync();
            logger.LogInformation("{Method} successful for endpoint {Endpoint}", methodName, endpoint);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result  = JsonSerializer.Deserialize<T>(responseContent, options);
            return (true, null, result);
        }

        var (success, errorMessage) = await HandleErrorResponse(response, methodName, endpoint);
        return (success, errorMessage, default);
    }

    private async Task<(bool success, string? errorMessage)> HandleErrorResponse(HttpResponseMessage response, string methodName, string endpoint) {
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest) {
            string errorContent = await response.Content.ReadAsStringAsync();

            var    options       = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var    errorResponse = JsonSerializer.Deserialize<ServiceLayerErrorResponse>(errorContent, options);
            string errorMessage  = errorResponse?.Error?.Message ?? errorResponse?.Error?.Details?[0]?.Message ?? "Unknown error";

            logger.LogWarning("{Method} failed for {Endpoint}: {ErrorMessage}", methodName, endpoint, errorMessage);
            return (false, errorMessage);
        }

        logger.LogError("{Method} failed for {Endpoint} with status {StatusCode}: {ReasonPhrase}", methodName, endpoint, response.StatusCode, response.ReasonPhrase);
        return (false, $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
    }
    private class LoginResponse {
        public string SessionId      { get; set; } = string.Empty;
        public string Version        { get; set; } = string.Empty;
        public int    SessionTimeout { get; set; }
    }

    private class ServiceLayerErrorResponse {
        public ServiceLayerError? Error { get; set; }
    }

    private class ServiceLayerError {
        public string Code { get; set; }
        public string Message { get; set; }
        public List<ServiceLayerErrorDetail> Details { get; set; } = new();
    }

    private class ServiceLayerErrorDetail {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}