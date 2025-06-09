using System.Text;
using System.Text.Json;
using Core.Interfaces;

namespace Adapters.CrossPlatform.SBO.Services;

public class SboCompany(ISettings settings) {
    private readonly string url      = settings.SboSettings?.ServiceLayerUrl ?? throw new InvalidOperationException("SBO service layer URL is not configured.");
    private readonly string user     = settings.SboSettings.User ?? throw new InvalidOperationException("SBO user is not configured.");
    private readonly string password = settings.SboSettings.Password ?? throw new InvalidOperationException("SBO password is not configured.");
    private readonly string database = settings.SboSettings.Database ?? throw new InvalidOperationException("SBO database is not configured.");
    
    private readonly HttpClient httpClient = new();
    private readonly SemaphoreSlim connectionSemaphore = new(1, 1);
    
    private string? sessionId;
    private DateTime sessionExpiry = DateTime.MinValue;
    
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
                    sessionId = loginResponse.SessionId;
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
        return !string.IsNullOrEmpty(sessionId) && DateTime.UtcNow < sessionExpiry;
    }
    
    private class LoginResponse {
        public string SessionId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int SessionTimeout { get; set; }
    }
}
