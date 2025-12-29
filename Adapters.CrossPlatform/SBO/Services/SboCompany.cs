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

            string errorContent = await response.Content.ReadAsStringAsync();
            string errorMessage;
            
            try {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var errorResponse = JsonSerializer.Deserialize<ServiceLayerErrorResponse>(errorContent, options);
                errorMessage = errorResponse?.Error?.Message ?? errorResponse?.Error?.Details?[0]?.Message ?? $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            }
            catch {
                errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            }

            logger.LogError("Failed to connect to Service Layer: {ErrorMessage}", errorMessage);
            throw new Exception($"SBO Service Layer Connection Error: {errorMessage}");
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

        logger.LogDebug("Sending to service layer method: {httpMethod} {endpoint} with body: {body}", httpMethod.Method, endpoint, json);
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
            logger.LogDebug("Service Layer response for {Endpoint}: {ResponseContent}", endpoint, responseContent);
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
            logger.LogDebug("Service Layer error response for {Endpoint}: {ResponseContent}", endpoint, errorContent);

            string errorMessage = ExtractErrorMessage(errorContent);

            logger.LogWarning("{Method} failed for {Endpoint}: {ErrorMessage}", methodName, endpoint, errorMessage);
            return (false, errorMessage);
        }

        logger.LogError("{Method} failed for {Endpoint} with status {StatusCode}: {ReasonPhrase}", methodName, endpoint, response.StatusCode, response.ReasonPhrase);
        return (false, $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
    }

    private string ExtractErrorMessage(string errorContent) {
        try {
            using var jsonDoc = JsonDocument.Parse(errorContent);
            var root = jsonDoc.RootElement;

            // Try to get error object
            if (root.TryGetProperty("error", out var errorElement)) {
                // Try message property (might be string or object)
                if (errorElement.TryGetProperty("message", out var messageElement)) {
                    if (messageElement.ValueKind == JsonValueKind.String) {
                        return messageElement.GetString() ?? "Unknown error";
                    }
                    else if (messageElement.ValueKind == JsonValueKind.Object) {
                        // Message is an object, try to extract text or value property
                        if (messageElement.TryGetProperty("text", out var textElement)) {
                            return textElement.GetString() ?? "Unknown error";
                        }
                        if (messageElement.TryGetProperty("value", out var valueElement)) {
                            return valueElement.GetString() ?? "Unknown error";
                        }
                    }
                }

                // Try details array
                if (errorElement.TryGetProperty("details", out var detailsElement) && detailsElement.ValueKind == JsonValueKind.Array) {
                    foreach (var detail in detailsElement.EnumerateArray()) {
                        if (detail.TryGetProperty("message", out var detailMessage) && detailMessage.ValueKind == JsonValueKind.String) {
                            return detailMessage.GetString() ?? "Unknown error";
                        }
                    }
                }
            }
        }
        catch {
            // If JSON parsing fails, fall back to generic message
        }

        return "Unknown error";
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
        public int? Code { get; set; }
        public string? Message { get; set; }
        public List<ServiceLayerErrorDetail> Details { get; set; } = new();
    }

    private class ServiceLayerErrorDetail {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public async Task<(bool success, string? errorMessage, List<BatchOperationResult> responses)> ExecuteBatchAsync(params BatchOperation[] operations) {
        await ConnectCompany();

        string batchBoundary = $"batch_{Guid.NewGuid()}";
        string changeSetBoundary = $"changeset_{Guid.NewGuid()}";

        var batchContent = new MultipartContent("mixed", batchBoundary);
        batchContent.Headers.ContentType!.Parameters.Clear();
        batchContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("boundary", batchBoundary));

        // Create changeset content
        var changeSetBuilder = new StringBuilder();
        changeSetBuilder.AppendLine($"--{changeSetBoundary}");

        for (int i = 0; i < operations.Length; i++) {
            var operation = operations[i];
            
            changeSetBuilder.AppendLine($"Content-Type: application/http");
            changeSetBuilder.AppendLine($"Content-Transfer-Encoding: binary");
            changeSetBuilder.AppendLine($"Content-ID: {i + 1}");
            changeSetBuilder.AppendLine();

            changeSetBuilder.AppendLine($"{operation.Method} /b1s/v2/{operation.Endpoint} HTTP/1.1");
            changeSetBuilder.AppendLine("Content-Type: application/json");
            changeSetBuilder.AppendLine($"Content-Length: {operation.Body.Length}");
            changeSetBuilder.AppendLine();
            changeSetBuilder.AppendLine(operation.Body);
            
            if (i < operations.Length - 1) {
                changeSetBuilder.AppendLine($"--{changeSetBoundary}");
            }
        }
        
        changeSetBuilder.AppendLine($"--{changeSetBoundary}--");

        var changeSetContent = new StringContent(changeSetBuilder.ToString());
        changeSetContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/mixed");
        changeSetContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("boundary", changeSetBoundary));

        batchContent.Add(changeSetContent);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{url}/b1s/v2/$batch");
        request.Headers.Add("Cookie", $"B1SESSION={SessionId};");
        request.Content = batchContent;

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
            string responseContent = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Batch operation successful");
            
            // Parse the multipart batch response
            var results = ParseBatchResponse(responseContent);
            
            // Check if all operations succeeded
            bool allSucceeded = results.All(r => r.StatusCode >= 200 && r.StatusCode < 300);
            if (!allSucceeded) {
                var firstError = results.FirstOrDefault(r => r.StatusCode >= 400);
                return (false, firstError?.ErrorMessage ?? "One or more batch operations failed", results);
            }
            
            return (true, null, results);
        }

        var (success, errorMessage) = await HandleBatchErrorResponse(response);
        return (success, errorMessage, new List<BatchOperationResult>());
    }

    private async Task<(bool success, string? errorMessage)> HandleBatchErrorResponse(HttpResponseMessage response) {
        string errorContent = await response.Content.ReadAsStringAsync();
        
        // Parse batch response to find the specific error
        // This is a simplified version - in production you'd parse the multipart response properly
        if (errorContent.Contains("\"error\"")) {
            try {
                // Extract JSON error from multipart response
                int startIndex = errorContent.IndexOf("{");
                int endIndex = errorContent.LastIndexOf("}") + 1;
                if (startIndex >= 0 && endIndex > startIndex) {
                    string jsonError = errorContent.Substring(startIndex, endIndex - startIndex);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var errorResponse = JsonSerializer.Deserialize<ServiceLayerErrorResponse>(jsonError, options);
                    string errorMessage = errorResponse?.Error?.Message ?? errorResponse?.Error?.Details?[0]?.Message ?? "Unknown batch error";
                    logger.LogError("Batch operation failed: {ErrorMessage}", errorMessage);
                    return (false, errorMessage);
                }
            }
            catch {
                // Fall back to generic error handling
            }
        }

        logger.LogError("Batch operation failed with status {StatusCode}", response.StatusCode);
        return (false, $"Batch operation failed: HTTP {response.StatusCode}");
    }

    private List<BatchOperationResult> ParseBatchResponse(string responseContent) {
        var results = new List<BatchOperationResult>();
        
        // Split by changeset boundaries
        var parts = responseContent.Split(new[] { "--changesetresponse_" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts.Skip(1)) { // Skip the first part which is before the first boundary
            if (part.Contains("HTTP/1.1")) {
                var result = new BatchOperationResult();
                
                // Extract status line
                var httpLineStart = part.IndexOf("HTTP/1.1");
                var httpLineEnd = part.IndexOf("\r\n", httpLineStart);
                if (httpLineEnd == -1) httpLineEnd = part.IndexOf("\n", httpLineStart);
                
                if (httpLineStart >= 0 && httpLineEnd > httpLineStart) {
                    var statusLine = part.Substring(httpLineStart, httpLineEnd - httpLineStart);
                    var statusParts = statusLine.Split(' ');
                    if (statusParts.Length >= 3) {
                        result.StatusCode = int.Parse(statusParts[1]);
                        result.StatusText = string.Join(" ", statusParts.Skip(2));
                    }
                }
                
                // Extract Content-ID
                var contentIdMatch = System.Text.RegularExpressions.Regex.Match(part, @"Content-ID:\s*(\d+)");
                if (contentIdMatch.Success) {
                    result.ContentId = int.Parse(contentIdMatch.Groups[1].Value);
                }
                
                // Extract JSON body if present
                var jsonStart = part.IndexOf("{");
                var jsonEnd = part.LastIndexOf("}");
                if (jsonStart >= 0 && jsonEnd > jsonStart) {
                    result.Body = part.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    
                    // Parse error if status indicates failure
                    if (result.StatusCode >= 400) {
                        try {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var errorResponse = JsonSerializer.Deserialize<ServiceLayerErrorResponse>(result.Body, options);
                            result.ErrorMessage = errorResponse?.Error?.Message ?? errorResponse?.Error?.Details?[0]?.Message;
                        }
                        catch {
                            // Ignore parsing errors
                        }
                    }
                }
                
                results.Add(result);
            }
        }
        
        return results;
    }

    public class BatchOperation {
        public string Method { get; set; } = "POST";
        public required string Endpoint { get; set; }
        public required string Body { get; set; }
    }
    
    public class BatchOperationResult {
        public int ContentId { get; set; }
        public int StatusCode { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string? Body { get; set; }
        public string? ErrorMessage { get; set; }
    }
}