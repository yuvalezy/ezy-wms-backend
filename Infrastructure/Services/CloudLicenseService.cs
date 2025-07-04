using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services;

public class CloudLicenseService(
    HttpClient httpClient,
    ISettings settings,
    SystemDbContext context,
    ILogger<CloudLicenseService> logger) : ICloudLicenseService {
    
    private readonly string _cloudEndpoint = settings.Licensing.CloudEndpoint ?? 
        throw new InvalidOperationException("Cloud endpoint not configured");
    private readonly string _bearerToken = settings.Licensing.BearerToken ?? 
        throw new InvalidOperationException("Bearer token not configured");

    public async Task<CloudLicenseResponse> SendDeviceEventAsync(CloudLicenseRequest request) {
        try {
            ConfigureHttpClient();
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{_cloudEndpoint}/api/license/device-event", content);
            
            if (response.IsSuccessStatusCode) {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CloudLicenseResponse>(responseJson) ?? 
                    new CloudLicenseResponse { Success = false, Message = "Failed to deserialize response" };
                
                logger.LogInformation("Device event {Event} sent successfully for device {DeviceUuid}", 
                    request.Event, request.DeviceUuid);
                
                return result;
            }
            else {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Cloud service returned error {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                
                return new CloudLicenseResponse {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex) {
            logger.LogError(ex, "Network error sending device event to cloud service");
            return new CloudLicenseResponse {
                Success = false,
                Message = "Network error: " + ex.Message
            };
        }
        catch (TaskCanceledException ex) {
            logger.LogError(ex, "Timeout sending device event to cloud service");
            return new CloudLicenseResponse {
                Success = false,
                Message = "Request timeout"
            };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Unexpected error sending device event to cloud service");
            return new CloudLicenseResponse {
                Success = false,
                Message = "Unexpected error: " + ex.Message
            };
        }
    }

    public async Task<AccountValidationResponse> ValidateAccountAsync(AccountValidationRequest request) {
        try {
            ConfigureHttpClient();
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{_cloudEndpoint}/api/license/validate-account", content);
            
            if (response.IsSuccessStatusCode) {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AccountValidationResponse>(responseJson) ??
                    new AccountValidationResponse { Success = false, Message = "Failed to deserialize response" };
                
                logger.LogInformation("Account validation completed successfully");
                return result;
            }
            else {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Account validation failed with status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                
                return new AccountValidationResponse {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during account validation");
            return new AccountValidationResponse {
                Success = false,
                Message = "Validation error: " + ex.Message
            };
        }
    }

    public async Task<bool> IsCloudAvailableAsync() {
        try {
            ConfigureHttpClient();
            var response = await httpClient.GetAsync($"{_cloudEndpoint}/api/license/health");
            return response.IsSuccessStatusCode;
        }
        catch {
            return false;
        }
    }

    public async Task QueueDeviceEventAsync(string eventType, string deviceUuid, string deviceName = "") {
        var request = new CloudLicenseRequest {
            DeviceUuid = deviceUuid,
            Event = eventType,
            DeviceName = deviceName,
            Timestamp = DateTime.UtcNow,
            AdditionalData = new Dictionary<string, object>()
        };

        var queueItem = new CloudSyncQueue {
            EventType = eventType,
            DeviceUuid = deviceUuid,
            RequestPayload = JsonSerializer.Serialize(request),
            RetryCount = 0,
            NextRetryAt = DateTime.UtcNow,
            Status = CloudSyncStatus.Pending
        };

        context.CloudSyncQueues.Add(queueItem);
        await context.SaveChangesAsync();

        logger.LogInformation("Queued device event {EventType} for device {DeviceUuid}", 
            eventType, deviceUuid);
    }

    public async Task ProcessQueuedEventsAsync() {
        var pendingItems = await context.CloudSyncQueues
            .Where(q => q.Status == CloudSyncStatus.Pending && q.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(q => q.CreatedAt)
            .Take(10) // Process up to 10 items at once
            .ToListAsync();

        foreach (var item in pendingItems) {
            await ProcessQueueItemAsync(item);
        }
    }

    public async Task<int> GetPendingEventCountAsync() {
        return await context.CloudSyncQueues
            .CountAsync(q => q.Status == CloudSyncStatus.Pending || q.Status == CloudSyncStatus.Failed);
    }

    private void ConfigureHttpClient() {
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _bearerToken);
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _bearerToken);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "WMS-License-Client/1.0");
        httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    private async Task ProcessQueueItemAsync(CloudSyncQueue item) {
        try {
            item.Status = CloudSyncStatus.Processing;
            item.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var request = JsonSerializer.Deserialize<CloudLicenseRequest>(item.RequestPayload);
            if (request == null) {
                throw new InvalidOperationException("Failed to deserialize request payload");
            }
            
            var response = await SendDeviceEventAsync(request);

            if (response.Success) {
                item.Status = CloudSyncStatus.Completed;
                item.ProcessedAt = DateTime.UtcNow;
                logger.LogInformation("Successfully processed queued event {EventType} for device {DeviceUuid}", 
                    item.EventType, item.DeviceUuid);
            }
            else {
                await HandleFailedQueueItemAsync(item, response.Message);
            }
        }
        catch (Exception ex) {
            await HandleFailedQueueItemAsync(item, ex.Message);
        }
        finally {
            item.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    private async Task HandleFailedQueueItemAsync(CloudSyncQueue item, string error) {
        item.RetryCount++;
        item.LastError = error;

        if (item.RetryCount >= 24) { // Max 24 retries (24 hours)
            item.Status = CloudSyncStatus.Abandoned;
            logger.LogError("Abandoned queued event {EventType} for device {DeviceUuid} after {RetryCount} retries", 
                item.EventType, item.DeviceUuid, item.RetryCount);
        }
        else {
            item.Status = CloudSyncStatus.Failed;
            item.NextRetryAt = DateTime.UtcNow.AddHours(1); // Retry every hour
            logger.LogWarning("Failed to process queued event {EventType} for device {DeviceUuid}. Retry {RetryCount}/24 scheduled", 
                item.EventType, item.DeviceUuid, item.RetryCount);
        }
    }
}