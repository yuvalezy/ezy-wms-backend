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
    
    private readonly HttpClient httpClient = CreateHttpClient();
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
            logger.LogDebug("Already connected to Service Layer with session {SessionId}", SessionId);
            return true;
        }
        
        logger.LogDebug("Waiting for connection semaphore...");
        await connectionSemaphore.WaitAsync();
        try {
            if (IsConnected()) {
                logger.LogDebug("Already connected after waiting for semaphore");
                return true;
            }
            
            logger.LogInformation("Connecting to Service Layer at {Url}", url);
            
            var loginData = new {
                CompanyDB = database,
                UserName = user,
                Password = password
            };
            
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            logger.LogDebug("POST {Url}/b1s/v2/Login\nBody: {Body}", url, json);
            
            var response = await httpClient.PostAsync($"{url}/b1s/v2/Login", content);
            
            if (response.IsSuccessStatusCode) {
                var responseContent = await response.Content.ReadAsStringAsync();
                logger.LogDebug("Login response: {Response}", responseContent);
                
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);
                
                if (loginResponse != null) {
                    SessionId = loginResponse.SessionId;
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
        
        var fullUrl = $"{url}/b1s/v2/{endpoint}";
        logger.LogDebug("GET {Url}\nCookie: B1SESSION={SessionId};", fullUrl, SessionId);
        
        var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");
        
        var response = await httpClient.SendAsync(request);
        
        if (response.IsSuccessStatusCode) {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogDebug("GET response: {Response}", content);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<T>(content, options);
        }
        
        logger.LogWarning("GET failed with status {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
        return default;
    }
    
    public async Task<(bool success, string? errorMessage)> PatchAsync<T>(string endpoint, T data) {
        await ConnectCompany();
        
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var fullUrl = $"{url}/b1s/v2/{endpoint}";
        logger.LogDebug("PATCH {Url}\nCookie: B1SESSION={SessionId};\nBody: {Body}", fullUrl, SessionId, json);
        
        var request = new HttpRequestMessage(HttpMethod.Patch, fullUrl);
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");
        request.Content = content;
        
        var response = await httpClient.SendAsync(request);
        
        if (response.IsSuccessStatusCode) {
            logger.LogInformation("PATCH successful for endpoint {Endpoint}", endpoint);
            return (true, null);
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest) {
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogDebug("PATCH error response: {ErrorContent}", errorContent);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var errorResponse = JsonSerializer.Deserialize<ServiceLayerErrorResponse>(errorContent, options);
            var errorMessage = errorResponse?.Error?.Message?.Value ?? "Unknown error";
            
            logger.LogWarning("PATCH failed for {Endpoint}: {ErrorMessage}", endpoint, errorMessage);
            return (false, errorMessage);
        }
        
        logger.LogError("PATCH failed for {Endpoint} with status {StatusCode}: {ReasonPhrase}", endpoint, response.StatusCode, response.ReasonPhrase);
        return (false, $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
    }
    
    public async Task<bool> DeleteAsync(string endpoint) {
        await ConnectCompany();
        
        var fullUrl = $"{url}/b1s/v2/{endpoint}";
        logger.LogDebug("DELETE {Url}\nCookie: B1SESSION={SessionId};", fullUrl, SessionId);
        
        var request = new HttpRequestMessage(HttpMethod.Delete, fullUrl);
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");
        
        var response = await httpClient.SendAsync(request);
        
        if (response.IsSuccessStatusCode) {
            logger.LogInformation("DELETE successful for endpoint {Endpoint}", endpoint);
        } else {
            logger.LogWarning("DELETE failed for {Endpoint} with status {StatusCode}: {ReasonPhrase}", endpoint, response.StatusCode, response.ReasonPhrase);
        }
        
        return response.IsSuccessStatusCode;
    }
    
    public async Task<(bool success, string? errorMessage, T? result)> PostAsync<T>(string endpoint, object data) {
        await ConnectCompany();
        
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var fullUrl = $"{url}/b1s/v2/{endpoint}";
        logger.LogDebug("POST {Url}\nCookie: B1SESSION={SessionId};\nBody: {Body}", fullUrl, SessionId, json);
        
        var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");
        request.Content = content;
        
        var response = await httpClient.SendAsync(request);
        
        if (response.IsSuccessStatusCode) {
            var responseContent = await response.Content.ReadAsStringAsync();
            logger.LogDebug("POST response: {Response}", responseContent);
            logger.LogInformation("POST successful for endpoint {Endpoint}", endpoint);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<T>(responseContent, options);
            return (true, null, result);
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest) {
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogDebug("POST error response: {ErrorContent}", errorContent);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var errorResponse = JsonSerializer.Deserialize<ServiceLayerErrorResponse>(errorContent, options);
            var errorMessage = errorResponse?.Error?.Message?.Value ?? "Unknown error";
            
            logger.LogWarning("POST failed for {Endpoint}: {ErrorMessage}", endpoint, errorMessage);
            return (false, errorMessage, default);
        }
        
        logger.LogError("POST failed for {Endpoint} with status {StatusCode}: {ReasonPhrase}", endpoint, response.StatusCode, response.ReasonPhrase);
        return (false, $"HTTP {response.StatusCode}: {response.ReasonPhrase}", default);
    }
    
    private class LoginResponse {
        public string SessionId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int SessionTimeout { get; set; }
    }
    
    private class ServiceLayerErrorResponse {
        public ServiceLayerError? Error { get; set; }
    }
    
    private class ServiceLayerError {
        public int Code { get; set; }
        public ServiceLayerMessage? Message { get; set; }
    }
    
    private class ServiceLayerMessage {
        public string Lang { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
