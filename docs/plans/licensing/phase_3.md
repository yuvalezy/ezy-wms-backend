# Phase 3: Cloud Integration & Synchronization

## Objectives
- Implement cloud license server communication
- Create background service for sync operations
- Implement retry and queue mechanisms for failed requests
- Add bearer token authentication with IP locking
- Create mock server for testing

## Technical Tasks

### 1. Cloud License Server Models

#### Cloud Request/Response Models
```csharp
public class CloudLicenseRequest
{
    public string DeviceUuid { get; set; }
    public string Event { get; set; } // register, activate, deactivate, disable
    public string DeviceName { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}

public class CloudLicenseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public LicenseData LicenseData { get; set; }
    public DateTime ServerTimestamp { get; set; }
}

public class LicenseData
{
    public AccountState AccountStatus { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? PaymentCycleDate { get; set; }
    public DateTime? DemoExpirationDate { get; set; }
    public string InactiveReason { get; set; }
    public int MaxAllowedDevices { get; set; }
    public int ActiveDeviceCount { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}

public class AccountValidationRequest
{
    public List<string> ActiveDeviceUuids { get; set; }
    public DateTime LastValidationTimestamp { get; set; }
}

public class AccountValidationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public LicenseData LicenseData { get; set; }
    public List<string> DevicesToDeactivate { get; set; }
    public DateTime ServerTimestamp { get; set; }
}
```

### 2. Cloud Queue Entity

#### CloudSyncQueue Entity
```csharp
using Core.Entities;

public class CloudSyncQueue : BaseEntity
{
    public string EventType { get; set; } // device_register, device_activate, etc.
    public string DeviceUuid { get; set; }
    public string RequestPayload { get; set; } // JSON serialized request
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime NextRetryAt { get; set; }
    public CloudSyncStatus Status { get; set; }
    public string LastError { get; set; }
}

public enum CloudSyncStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Abandoned = 5
}
```

### 3. Cloud License Service

#### ICloudLicenseService Interface
```csharp
public interface ICloudLicenseService
{
    Task<CloudLicenseResponse> SendDeviceEventAsync(CloudLicenseRequest request);
    Task<AccountValidationResponse> ValidateAccountAsync(AccountValidationRequest request);
    Task<bool> IsCloudAvailableAsync();
    Task QueueDeviceEventAsync(string eventType, string deviceUuid, string deviceName = null);
    Task ProcessQueuedEventsAsync();
    Task<int> GetPendingEventCountAsync();
}
```

#### CloudLicenseService Implementation
```csharp
public class CloudLicenseService : ICloudLicenseService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly LicenseDbContext _context;
    private readonly ILogger<CloudLicenseService> _logger;
    private readonly string _cloudEndpoint;
    private readonly string _bearerToken;

    public CloudLicenseService(
        HttpClient httpClient,
        IConfiguration configuration,
        LicenseDbContext context,
        ILogger<CloudLicenseService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _context = context;
        _logger = logger;
        
        _cloudEndpoint = configuration["Licensing:CloudEndpoint"] ?? 
            throw new InvalidOperationException("Cloud endpoint not configured");
        _bearerToken = configuration["Licensing:BearerToken"] ?? 
            throw new InvalidOperationException("Bearer token not configured");
        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _bearerToken);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "WMS-License-Client/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<CloudLicenseResponse> SendDeviceEventAsync(CloudLicenseRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_cloudEndpoint}/api/license/device-event", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonUtils.Deserialize<CloudLicenseResponse>(responseJson);
                
                _logger.LogInformation("Device event {Event} sent successfully for device {DeviceUuid}", 
                    request.Event, request.DeviceUuid);
                
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Cloud service returned error {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                
                return new CloudLicenseResponse
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error sending device event to cloud service");
            return new CloudLicenseResponse
            {
                Success = false,
                Message = "Network error: " + ex.Message
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout sending device event to cloud service");
            return new CloudLicenseResponse
            {
                Success = false,
                Message = "Request timeout"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending device event to cloud service");
            return new CloudLicenseResponse
            {
                Success = false,
                Message = "Unexpected error: " + ex.Message
            };
        }
    }

    public async Task<AccountValidationResponse> ValidateAccountAsync(AccountValidationRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_cloudEndpoint}/api/license/validate-account", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonUtils.Deserialize<AccountValidationResponse>(responseJson);
                
                _logger.LogInformation("Account validation completed successfully");
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Account validation failed with status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                
                return new AccountValidationResponse
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during account validation");
            return new AccountValidationResponse
            {
                Success = false,
                Message = "Validation error: " + ex.Message
            };
        }
    }

    public async Task<bool> IsCloudAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_cloudEndpoint}/api/license/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task QueueDeviceEventAsync(string eventType, string deviceUuid, string deviceName = null)
    {
        var request = new CloudLicenseRequest
        {
            DeviceUuid = deviceUuid,
            Event = eventType,
            DeviceName = deviceName,
            Timestamp = DateTime.UtcNow,
            AdditionalData = new Dictionary<string, object>()
        };

        var queueItem = new CloudSyncQueue
        {
            EventType = eventType,
            DeviceUuid = deviceUuid,
            RequestPayload = JsonSerializer.Serialize(request),
            RetryCount = 0,
            NextRetryAt = DateTime.UtcNow,
            Status = CloudSyncStatus.Pending
        };

        _context.CloudSyncQueue.Add(queueItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Queued device event {EventType} for device {DeviceUuid}", 
            eventType, deviceUuid);
    }

    public async Task ProcessQueuedEventsAsync()
    {
        var pendingItems = await _context.CloudSyncQueue
            .Where(q => q.Status == CloudSyncStatus.Pending && q.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(q => q.CreatedAt)
            .Take(10) // Process up to 10 items at once
            .ToListAsync();

        foreach (var item in pendingItems)
        {
            await ProcessQueueItemAsync(item);
        }
    }

    private async Task ProcessQueueItemAsync(CloudSyncQueue item)
    {
        try
        {
            item.Status = CloudSyncStatus.Processing;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var request = JsonUtils.Deserialize<CloudLicenseRequest>(item.RequestPayload);
            var response = await SendDeviceEventAsync(request);

            if (response.Success)
            {
                item.Status = CloudSyncStatus.Completed;
                item.ProcessedAt = DateTime.UtcNow;
                _logger.LogInformation("Successfully processed queued event {EventType} for device {DeviceUuid}", 
                    item.EventType, item.DeviceUuid);
            }
            else
            {
                await HandleFailedQueueItemAsync(item, response.Message);
            }
        }
        catch (Exception ex)
        {
            await HandleFailedQueueItemAsync(item, ex.Message);
        }
        finally
        {
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private async Task HandleFailedQueueItemAsync(CloudSyncQueue item, string error)
    {
        item.RetryCount++;
        item.LastError = error;

        if (item.RetryCount >= 24) // Max 24 retries (24 hours)
        {
            item.Status = CloudSyncStatus.Abandoned;
            _logger.LogError("Abandoned queued event {EventType} for device {DeviceUuid} after {RetryCount} retries", 
                item.EventType, item.DeviceUuid, item.RetryCount);
        }
        else
        {
            item.Status = CloudSyncStatus.Failed;
            item.NextRetryAt = DateTime.UtcNow.AddHours(1); // Retry every hour
            _logger.LogWarning("Failed to process queued event {EventType} for device {DeviceUuid}. Retry {RetryCount}/24 scheduled", 
                item.EventType, item.DeviceUuid, item.RetryCount);
        }
    }

    public async Task<int> GetPendingEventCountAsync()
    {
        return await _context.CloudSyncQueue
            .CountAsync(q => q.Status == CloudSyncStatus.Pending || q.Status == CloudSyncStatus.Failed);
    }
}
```

### 4. Background Sync Service

#### CloudSyncBackgroundService
```csharp
public class CloudSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CloudSyncBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _syncInterval;
    private readonly TimeSpan _validationInterval;

    public CloudSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CloudSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        _syncInterval = TimeSpan.FromMinutes(configuration.GetValue<int>("Licensing:SyncIntervalMinutes", 10));
        _validationInterval = TimeSpan.FromHours(configuration.GetValue<int>("Licensing:ValidationIntervalHours", 24));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cloud sync background service started");
        
        var syncTimer = new Timer(ProcessSyncQueue, null, TimeSpan.Zero, _syncInterval);
        var validationTimer = new Timer(ProcessDailyValidation, null, TimeSpan.Zero, _validationInterval);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cloud sync background service is stopping");
        }
        finally
        {
            syncTimer?.Dispose();
            validationTimer?.Dispose();
        }
    }

    private async void ProcessSyncQueue(object state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cloudService = scope.ServiceProvider.GetRequiredService<ICloudLicenseService>();
            
            await cloudService.ProcessQueuedEventsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sync queue");
        }
    }

    private async void ProcessDailyValidation(object state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cloudService = scope.ServiceProvider.GetRequiredService<ICloudLicenseService>();
            var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();
            var accountStatusService = scope.ServiceProvider.GetRequiredService<IAccountStatusService>();
            var licenseCacheService = scope.ServiceProvider.GetRequiredService<ILicenseCacheService>();
            
            _logger.LogInformation("Starting daily account validation");
            
            // Get all active devices
            var devices = await deviceService.GetAllDevicesAsync();
            var activeDevices = devices.Where(d => d.Status == DeviceStatus.Active).ToList();
            
            var validationRequest = new AccountValidationRequest
            {
                ActiveDeviceUuids = activeDevices.Select(d => d.DeviceUuid).ToList(),
                LastValidationTimestamp = await licenseCacheService.GetLastValidationTimestampAsync() ?? DateTime.MinValue
            };
            
            var response = await cloudService.ValidateAccountAsync(validationRequest);
            
            if (response.Success && response.LicenseData != null)
            {
                // Update account status
                await accountStatusService.UpdateAccountStatusAsync(response.LicenseData.AccountStatus, 
                    "Daily validation from cloud service");
                
                // Update license cache
                var cacheData = new LicenseCacheData
                {
                    AccountStatus = response.LicenseData.AccountStatus,
                    ExpirationDate = response.LicenseData.ExpirationDate,
                    PaymentCycleDate = response.LicenseData.PaymentCycleDate,
                    DemoExpirationDate = response.LicenseData.DemoExpirationDate,
                    InactiveReason = response.LicenseData.InactiveReason,
                    LastValidationTimestamp = DateTime.UtcNow,
                    ActiveDeviceCount = response.LicenseData.ActiveDeviceCount,
                    MaxAllowedDevices = response.LicenseData.MaxAllowedDevices,
                    AdditionalData = response.LicenseData.AdditionalData ?? new Dictionary<string, object>()
                };
                
                await licenseCacheService.UpdateLicenseCacheAsync(cacheData);
                
                // Deactivate devices if requested
                if (response.DevicesToDeactivate?.Any() == true)
                {
                    foreach (var deviceUuid in response.DevicesToDeactivate)
                    {
                        // System action - pass null for sessionInfo
                        await deviceService.UpdateDeviceStatusAsync(deviceUuid, DeviceStatus.Disabled, 
                            "Deactivated by cloud service", null);
                    }
                }
                
                _logger.LogInformation("Daily validation completed successfully");
            }
            else
            {
                _logger.LogWarning("Daily validation failed: {Message}", response.Message);
                
                // Check if we should transition to PaymentDueUnknown
                var accountStatus = await accountStatusService.GetCurrentAccountStatusAsync();
                if (accountStatus.Status == AccountState.PaymentDue)
                {
                    await accountStatusService.UpdateAccountStatusAsync(AccountState.PaymentDueUnknown, 
                        "Cloud service unreachable during validation");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during daily validation");
        }
    }
}
```

### 5. Enhanced Device Service Integration

#### Updated IDeviceService
```csharp
public interface IDeviceService
{
    // ... existing methods ...
    Task<Device> RegisterDeviceAsync(string deviceUuid, string deviceName, SessionInfo sessionInfo, bool notifyCloud = true);
    Task<Device> UpdateDeviceStatusAsync(string deviceUuid, DeviceStatus status, string reason, SessionInfo sessionInfo, bool notifyCloud = true);
}
```

#### Enhanced DeviceService
```csharp
public class DeviceService : IDeviceService
{
    private readonly LicenseDbContext _context;
    private readonly ICloudLicenseService _cloudService;
    private readonly ILogger<DeviceService> _logger;

    public DeviceService(
        LicenseDbContext context,
        ICloudLicenseService cloudService,
        ILogger<DeviceService> logger)
    {
        _context = context;
        _cloudService = cloudService;
        _logger = logger;
    }

    public async Task<Device> RegisterDeviceAsync(string deviceUuid, string deviceName, SessionInfo sessionInfo, bool notifyCloud = true)
    {
        // ... existing registration logic ...

        if (notifyCloud)
        {
            await _cloudService.QueueDeviceEventAsync("register", deviceUuid, deviceName);
        }

        return device;
    }

    public async Task<Device> UpdateDeviceStatusAsync(string deviceUuid, DeviceStatus status, string reason, SessionInfo sessionInfo, bool notifyCloud = true)
    {
        // ... existing update logic ...

        if (notifyCloud)
        {
            string eventType = status switch
            {
                DeviceStatus.Active => "activate",
                DeviceStatus.Inactive => "deactivate",
                DeviceStatus.Disabled => "disable",
                _ => "update"
            };

            await _cloudService.QueueDeviceEventAsync(eventType, deviceUuid);
        }

        return device;
    }

    // ... rest of the existing methods ...
}
```

### 6. Mock Cloud Server

#### MockCloudServer.csx (C# Script)
```csharp
#!/usr/bin/env dotnet-script

#r "nuget: Microsoft.AspNetCore.App, 8.0.0"
#r "nuget: System.Text.Json, 8.0.0"

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Global variables to simulate server state
var deviceRegistry = new Dictionary<string, DeviceInfo>();
var accountState = new AccountInfo
{
    Status = "Active",
    ExpirationDate = DateTime.UtcNow.AddDays(30),
    PaymentCycleDate = DateTime.UtcNow.AddDays(30),
    MaxAllowedDevices = 5,
    ActiveDeviceCount = 0
};

app.MapPost("/api/license/device-event", async (HttpContext context) =>
{
    var bearerToken = context.Request.Headers["Authorization"]
        .FirstOrDefault()?.Replace("Bearer ", "");
    
    if (bearerToken != "test-token-123")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var request = JsonUtils.Deserialize<CloudLicenseRequest>(requestBody);

    // Simulate processing
    await Task.Delay(100);

    var response = new CloudLicenseResponse
    {
        Success = true,
        Message = $"Device event '{request.Event}' processed successfully",
        ServerTimestamp = DateTime.UtcNow
    };

    // Update device registry
    switch (request.Event)
    {
        case "register":
            deviceRegistry[request.DeviceUuid] = new DeviceInfo
            {
                DeviceUuid = request.DeviceUuid,
                DeviceName = request.DeviceName,
                Status = "Active",
                RegistrationDate = DateTime.UtcNow
            };
            accountState.ActiveDeviceCount++;
            break;
        case "deactivate":
        case "disable":
            if (deviceRegistry.ContainsKey(request.DeviceUuid))
            {
                deviceRegistry[request.DeviceUuid].Status = "Inactive";
                accountState.ActiveDeviceCount--;
            }
            break;
        case "activate":
            if (deviceRegistry.ContainsKey(request.DeviceUuid))
            {
                deviceRegistry[request.DeviceUuid].Status = "Active";
                accountState.ActiveDeviceCount++;
            }
            break;
    }

    response.LicenseData = new LicenseData
    {
        AccountStatus = accountState.Status,
        ExpirationDate = accountState.ExpirationDate,
        PaymentCycleDate = accountState.PaymentCycleDate,
        MaxAllowedDevices = accountState.MaxAllowedDevices,
        ActiveDeviceCount = accountState.ActiveDeviceCount,
        AdditionalData = new Dictionary<string, object>()
    };

    var responseJson = JsonSerializer.Serialize(response);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(responseJson);
});

app.MapPost("/api/license/validate-account", async (HttpContext context) =>
{
    var bearerToken = context.Request.Headers["Authorization"]
        .FirstOrDefault()?.Replace("Bearer ", "");
    
    if (bearerToken != "test-token-123")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var request = JsonUtils.Deserialize<AccountValidationRequest>(requestBody);

    // Simulate validation processing
    await Task.Delay(200);

    var response = new AccountValidationResponse
    {
        Success = true,
        Message = "Account validation successful",
        ServerTimestamp = DateTime.UtcNow,
        LicenseData = new LicenseData
        {
            AccountStatus = accountState.Status,
            ExpirationDate = accountState.ExpirationDate,
            PaymentCycleDate = accountState.PaymentCycleDate,
            MaxAllowedDevices = accountState.MaxAllowedDevices,
            ActiveDeviceCount = accountState.ActiveDeviceCount,
            AdditionalData = new Dictionary<string, object>()
        },
        DevicesToDeactivate = new List<string>() // No devices to deactivate in mock
    };

    var responseJson = JsonSerializer.Serialize(response);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(responseJson);
});

app.MapGet("/api/license/health", async (HttpContext context) =>
{
    var response = new { Status = "OK", Timestamp = DateTime.UtcNow };
    var responseJson = JsonSerializer.Serialize(response);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(responseJson);
});

app.MapGet("/api/license/admin/devices", async (HttpContext context) =>
{
    var responseJson = JsonSerializer.Serialize(deviceRegistry.Values);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(responseJson);
});

app.MapPost("/api/license/admin/account-status", async (HttpContext context) =>
{
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var request = JsonUtils.Deserialize<Dictionary<string, object>>(requestBody);
    
    if (request.ContainsKey("status"))
    {
        accountState.Status = request["status"].ToString();
    }
    if (request.ContainsKey("expirationDate"))
    {
        accountState.ExpirationDate = DateTime.Parse(request["expirationDate"].ToString());
    }
    
    context.Response.StatusCode = 200;
    await context.Response.WriteAsync("Account status updated");
});

Console.WriteLine("Mock Cloud License Server starting...");
Console.WriteLine("Available endpoints:");
Console.WriteLine("  POST /api/license/device-event");
Console.WriteLine("  POST /api/license/validate-account");
Console.WriteLine("  GET  /api/license/health");
Console.WriteLine("  GET  /api/license/admin/devices");
Console.WriteLine("  POST /api/license/admin/account-status");
Console.WriteLine();
Console.WriteLine("Use Bearer token: test-token-123");
Console.WriteLine("Press Ctrl+C to stop the server");

await app.RunAsync();

// Helper classes
public class DeviceInfo
{
    public string DeviceUuid { get; set; }
    public string DeviceName { get; set; }
    public string Status { get; set; }
    public DateTime RegistrationDate { get; set; }
}

public class AccountInfo
{
    public string Status { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? PaymentCycleDate { get; set; }
    public int MaxAllowedDevices { get; set; }
    public int ActiveDeviceCount { get; set; }
}

// Request/Response classes (simplified versions)
public class CloudLicenseRequest
{
    public string DeviceUuid { get; set; }
    public string Event { get; set; }
    public string DeviceName { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CloudLicenseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public LicenseData LicenseData { get; set; }
    public DateTime ServerTimestamp { get; set; }
}

public class LicenseData
{
    public string AccountStatus { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? PaymentCycleDate { get; set; }
    public int MaxAllowedDevices { get; set; }
    public int ActiveDeviceCount { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}

public class AccountValidationRequest
{
    public List<string> ActiveDeviceUuids { get; set; }
    public DateTime LastValidationTimestamp { get; set; }
}

public class AccountValidationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public LicenseData LicenseData { get; set; }
    public List<string> DevicesToDeactivate { get; set; }
    public DateTime ServerTimestamp { get; set; }
}
```

### 7. Configuration Updates

#### appsettings.json
```json
{
  "Licensing": {
    "CloudEndpoint": "https://license.example.com",
    "BearerToken": "your-secure-bearer-token",
    "SyncIntervalMinutes": 10,
    "ValidationIntervalHours": 24,
    "MaxRetryAttempts": 24,
    "RetryIntervalHours": 1
  }
}
```

#### Startup.cs Updates
```csharp
services.AddHttpClient<ICloudLicenseService, CloudLicenseService>();
services.AddScoped<ICloudLicenseService, CloudLicenseService>();
services.AddHostedService<CloudSyncBackgroundService>();
```

### 8. Database Schema Notes

When ready for database migration, the following table will need to be created:

**CloudSyncQueue Table**
- Inherits from BaseEntity
- EventType, DeviceUuid, RequestPayload
- ProcessedAt, RetryCount, NextRetryAt
- Status (enum), LastError
- Indexes on Status/NextRetryAt and DeviceUuid

## Testing Approach

### Unit Tests
```csharp
[TestFixture]
public class CloudLicenseServiceTests
{
    [Test]
    public async Task SendDeviceEvent_ValidRequest_ReturnsSuccess()
    {
        // Test cloud communication
    }

    [Test]
    public async Task QueueDeviceEvent_ValidEvent_AddsToQueue()
    {
        // Test event queuing
    }

    [Test]
    public async Task ProcessQueuedEvents_WithRetries_HandlesFailures()
    {
        // Test retry logic
    }
}

[TestFixture]
public class CloudSyncBackgroundServiceTests
{
    [Test]
    public async Task ProcessSyncQueue_WithPendingEvents_ProcessesSuccessfully()
    {
        // Test background processing
    }

    [Test]
    public async Task ProcessDailyValidation_WithCloudResponse_UpdatesLocalState()
    {
        // Test daily validation
    }
}
```

### Integration Tests
```csharp
[Test]
public async Task MockCloudServer_DeviceRegistration_WorksEndToEnd()
{
    // Test with mock server
}

[Test]
public async Task CloudIntegration_NetworkFailure_QueuesForRetry()
{
    // Test network failure scenarios
}
```

### Mock Server Testing
```bash
# Start mock server
dotnet script MockCloudServer.csx

# Test device registration
curl -X POST http://localhost:5000/api/license/device-event \
  -H "Authorization: Bearer test-token-123" \
  -H "Content-Type: application/json" \
  -d '{"DeviceUuid":"test-device-001","Event":"register","DeviceName":"Test Device","Timestamp":"2024-01-01T00:00:00Z"}'

# Test account validation
curl -X POST http://localhost:5000/api/license/validate-account \
  -H "Authorization: Bearer test-token-123" \
  -H "Content-Type: application/json" \
  -d '{"ActiveDeviceUuids":["test-device-001"],"LastValidationTimestamp":"2024-01-01T00:00:00Z"}'
```

## Success Criteria

- [ ] Cloud license service communicates successfully with external server
- [ ] Device events are queued and processed reliably
- [ ] Retry mechanism handles network failures gracefully
- [ ] Daily validation updates local license state
- [ ] Background service processes queued events on schedule
- [ ] Mock server enables comprehensive testing
- [ ] All unit and integration tests pass
- [ ] Network failure scenarios handled correctly

## Dependencies

### Phase 1 & 2 Components
- Device management system
- Account status management
- License caching system
- Database context

### External Dependencies
- HTTP client for cloud communication
- Background service hosting
- Configuration system for endpoints and tokens

## Deliverables

1. Cloud license service implementation
2. Background synchronization service
3. Queue management system
4. Mock cloud server for testing
5. Database migration for queue table
6. Configuration updates
7. Unit and integration tests
8. Documentation for cloud integration

## Next Phase

Phase 4 will implement access control middleware and complete the licensing system integration with the main application.