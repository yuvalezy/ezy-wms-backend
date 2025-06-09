using System.Text;
using System.Text.Json;
using Core.Interfaces;

namespace Adapters.CrossPlatform.SBO.Services;

public class SboCompany(ISettings settings) {
    private readonly string url      = settings.SboSettings?.ServiceLayerUrl ?? throw new InvalidOperationException("SBO service layer URL is not configured.");
    private readonly string user     = settings.SboSettings.User ?? throw new InvalidOperationException("SBO user is not configured.");
    private readonly string password = settings.SboSettings.Password ?? throw new InvalidOperationException("SBO password is not configured.");
    private readonly string database = settings.SboSettings.Database ?? throw new InvalidOperationException("SBO database is not configured.");
    
    private readonly HttpClient httpClient = CreateHttpClient();
    private readonly SemaphoreSlim connectionSemaphore = new(1, 1);
    
    private static HttpClient CreateHttpClient() {
        var handler = new HttpClientHandler() {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
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
            
            var loginData = new {
                CompanyDB = database,
                UserName = user,
                Password = password
            };
            
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{url}/b1s/v1/Login", content);
            
            if (response.IsSuccessStatusCode) {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);
                
                if (loginResponse != null) {
                    SessionId = loginResponse.SessionId;
                    sessionExpiry = DateTime.UtcNow.AddMinutes(loginResponse.SessionTimeout);
                    return true;
                }
            }
            
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
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"{url}/b1s/v1/{endpoint}");
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");
        
        var response = await httpClient.SendAsync(request);
        
        if (response.IsSuccessStatusCode) {
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<T>(content, options);
        }
        
        return default;
    }
    
    public async Task<(bool success, string? errorMessage)> PatchAsync<T>(string endpoint, T data) {
        await ConnectCompany();
        
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{url}/b1s/v1/{endpoint}");
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");
        request.Content = content;
        
        var response = await httpClient.SendAsync(request);
        
        if (response.IsSuccessStatusCode) {
            return (true, null);
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest) {
            var errorContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var errorResponse = JsonSerializer.Deserialize<ServiceLayerErrorResponse>(errorContent, options);
            return (false, errorResponse?.Error?.Message?.Value ?? "Unknown error");
        }
        
        return (false, $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
    }
    
    public async Task<bool> DeleteAsync(string endpoint) {
        await ConnectCompany();
        
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{url}/b1s/v1/{endpoint}");
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");
        
        var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
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
