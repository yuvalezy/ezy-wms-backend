# Phase 4: Access Control & Middleware

## Objectives
- Implement license validation middleware
- Enforce API endpoint restrictions based on license status
- Integrate with existing authentication system
- Add license warnings to authentication responses
- Complete system integration and testing

## Technical Tasks

### 1. License Validation Middleware

#### LicenseValidationMiddleware
```csharp
public class LicenseValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LicenseValidationMiddleware> _logger;
    private readonly HashSet<string> _allowedEndpoints;
    private readonly HashSet<string> _superUserOnlyEndpoints;

    public LicenseValidationMiddleware(
        RequestDelegate next, 
        ILogger<LicenseValidationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        
        // Endpoints that are always allowed (even for non-activated devices)
        _allowedEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/api/authentication/login",
            "/api/authentication/logout",
            "/api/authentication/companyname",
            "/api/users",
            "/api/authorization-groups",
            "/api/device",
            "/api/license/status",
            "/swagger",
            "/health"
        };

        // Endpoints that require superuser access
        _superUserOnlyEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/api/device"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        
        // Skip validation for allowed endpoints
        if (IsAllowedEndpoint(path))
        {
            await _next(context);
            return;
        }

        // Check if device is activated
        var deviceUuid = GetDeviceUuid(context);
        if (string.IsNullOrEmpty(deviceUuid))
        {
            await WriteErrorResponse(context, 400, "Device UUID not provided");
            return;
        }

        // Validate license for this device
        using var scope = context.RequestServices.CreateScope();
        var licenseValidationService = scope.ServiceProvider.GetRequiredService<ILicenseValidationService>();
        
        var isValidDevice = await licenseValidationService.ValidateDeviceAccessAsync(deviceUuid);
        if (!isValidDevice)
        {
            await WriteErrorResponse(context, 403, "Device not active or license invalid");
            return;
        }

        // Check system-wide access
        var systemAccess = await licenseValidationService.ValidateSystemAccessAsync();
        if (!systemAccess)
        {
            await WriteErrorResponse(context, 403, "System access denied due to license status");
            return;
        }

        await _next(context);
    }

    private bool IsAllowedEndpoint(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return _allowedEndpoints.Any(endpoint => 
            path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private string GetDeviceUuid(HttpContext context)
    {
        // Try to get from header first
        var deviceUuid = context.Request.Headers["X-Device-UUID"].FirstOrDefault();
        
        // If not in header, try to get from session
        if (string.IsNullOrEmpty(deviceUuid))
        {
            var sessionInfo = context.GetSession();
            if (sessionInfo != null)
            {
                deviceUuid = sessionInfo.DeviceUuid;
            }
        }

        return deviceUuid;
    }

    private async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = message,
            timestamp = DateTime.UtcNow,
            path = context.Request.Path.Value
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        _logger.LogWarning("License validation failed for path {Path}: {Message}", 
            context.Request.Path.Value, message);
    }
}
```

### 2. Enhanced Authentication Controller

#### Updated AuthenticationController
```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILicenseValidationService _licenseValidationService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authService,
        ILicenseValidationService licenseValidationService,
        ILogger<AuthenticationController> logger)
    {
        _authService = authService;
        _licenseValidationService = licenseValidationService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request.Password);
            
            if (result.Success)
            {
                var response = new LoginResponse
                {
                    Token = result.Token,
                    SessionInfo = result.SessionInfo,
                    IsSuccess = true
                };

                // Add license warnings if applicable
                var licenseWarnings = await GetLicenseWarningsAsync();
                if (licenseWarnings.Any())
                {
                    response.LicenseWarnings = licenseWarnings;
                }

                return Ok(response);
            }
            else
            {
                return Unauthorized(new { error = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("companyname")]
    public async Task<ActionResult<CompanyNameResponse>> GetCompanyName()
    {
        try
        {
            var companyName = await _authService.GetCompanyNameAsync();
            var response = new CompanyNameResponse
            {
                CompanyName = companyName,
                ServerTime = DateTime.UtcNow
            };

            // Add license warnings if past due by 3+ days
            var licenseValidation = await _licenseValidationService.GetLicenseValidationResultAsync();
            if (licenseValidation.ShowWarning && licenseValidation.DaysUntilExpiration <= -3)
            {
                response.LicenseWarnings = new List<string> { licenseValidation.WarningMessage };
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company name");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("license-status")]
    public async Task<ActionResult<LicenseStatusResponse>> GetLicenseStatus()
    {
        try
        {
            var validation = await _licenseValidationService.GetLicenseValidationResultAsync();
            
            return Ok(new LicenseStatusResponse
            {
                IsValid = validation.IsValid,
                AccountStatus = validation.AccountStatus.ToString(),
                ExpirationDate = validation.ExpirationDate,
                DaysUntilExpiration = validation.DaysUntilExpiration,
                IsInGracePeriod = validation.IsInGracePeriod,
                WarningMessage = validation.WarningMessage,
                ShowWarning = validation.ShowWarning
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting license status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task<List<string>> GetLicenseWarningsAsync()
    {
        var warnings = new List<string>();
        
        try
        {
            var validation = await _licenseValidationService.GetLicenseValidationResultAsync();
            
            if (validation.ShowWarning)
            {
                warnings.Add(validation.WarningMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting license warnings");
        }

        return warnings;
    }
}
```

### 3. License Status API Controller

#### LicenseController
```csharp
[ApiController]
[Route("api/[controller]")]
public class LicenseController : ControllerBase
{
    private readonly ILicenseValidationService _licenseValidationService;
    private readonly IAccountStatusService _accountStatusService;
    private readonly ICloudLicenseService _cloudService;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(
        ILicenseValidationService licenseValidationService,
        IAccountStatusService accountStatusService,
        ICloudLicenseService cloudService,
        IDeviceService deviceService,
        ILogger<LicenseController> logger)
    {
        _licenseValidationService = licenseValidationService;
        _accountStatusService = accountStatusService;
        _cloudService = cloudService;
        _deviceService = deviceService;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<ActionResult<LicenseStatusResponse>> GetStatus()
    {
        try
        {
            var validation = await _licenseValidationService.GetLicenseValidationResultAsync();
            
            return Ok(new LicenseStatusResponse
            {
                IsValid = validation.IsValid,
                AccountStatus = validation.AccountStatus.ToString(),
                ExpirationDate = validation.ExpirationDate,
                DaysUntilExpiration = validation.DaysUntilExpiration,
                IsInGracePeriod = validation.IsInGracePeriod,
                WarningMessage = validation.WarningMessage,
                ShowWarning = validation.ShowWarning
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting license status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("queue-status")]
    [RequireSuperUser]
    public async Task<ActionResult<QueueStatusResponse>> GetQueueStatus()
    {
        try
        {
            var pendingCount = await _cloudService.GetPendingEventCountAsync();
            var cloudAvailable = await _cloudService.IsCloudAvailableAsync();
            
            return Ok(new QueueStatusResponse
            {
                PendingEventCount = pendingCount,
                CloudServiceAvailable = cloudAvailable,
                LastChecked = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("force-sync")]
    [RequireSuperUser]
    public async Task<ActionResult> ForceSync()
    {
        try
        {
            await _cloudService.ProcessQueuedEventsAsync();
            return Ok(new { message = "Sync initiated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing sync");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("validate-device")]
    public async Task<ActionResult<DeviceValidationResponse>> ValidateDevice([FromBody] DeviceValidationRequest request)
    {
        try
        {
            var isValid = await _licenseValidationService.ValidateDeviceAccessAsync(request.DeviceUuid);
            var device = await _deviceService.GetDeviceAsync(request.DeviceUuid);
            
            return Ok(new DeviceValidationResponse
            {
                IsValid = isValid,
                DeviceStatus = device?.Status.ToString(),
                ValidationTimestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating device");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
```

### 4. Response Models

#### License Response DTOs
```csharp
public class LoginResponse
{
    public string Token { get; set; }
    public bool IsSuccess { get; set; }
    public object SessionInfo { get; set; }
    public List<string> LicenseWarnings { get; set; } = new List<string>();
}

public class CompanyNameResponse
{
    public string CompanyName { get; set; }
    public DateTime ServerTime { get; set; }
    public List<string> LicenseWarnings { get; set; } = new List<string>();
}

public class LicenseStatusResponse
{
    public bool IsValid { get; set; }
    public string AccountStatus { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int DaysUntilExpiration { get; set; }
    public bool IsInGracePeriod { get; set; }
    public string WarningMessage { get; set; }
    public bool ShowWarning { get; set; }
}

public class QueueStatusResponse
{
    public int PendingEventCount { get; set; }
    public bool CloudServiceAvailable { get; set; }
    public DateTime LastChecked { get; set; }
}

public class DeviceValidationRequest
{
    public string DeviceUuid { get; set; }
}

public class DeviceValidationResponse
{
    public bool IsValid { get; set; }
    public string DeviceStatus { get; set; }
    public DateTime ValidationTimestamp { get; set; }
}
```

### 5. Error Handling Middleware

#### LicenseErrorHandlingMiddleware
```csharp
public class LicenseErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LicenseErrorHandlingMiddleware> _logger;

    public LicenseErrorHandlingMiddleware(RequestDelegate next, ILogger<LicenseErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (LicenseValidationException ex)
        {
            await HandleLicenseExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in license middleware");
            throw;
        }
    }

    private async Task HandleLicenseExceptionAsync(HttpContext context, LicenseValidationException ex)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "License validation failed",
            message = ex.Message,
            licenseStatus = ex.LicenseStatus,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        _logger.LogWarning("License validation exception: {Message}", ex.Message);
    }
}

public class LicenseValidationException : Exception
{
    public string LicenseStatus { get; }

    public LicenseValidationException(string message, string licenseStatus) : base(message)
    {
        LicenseStatus = licenseStatus;
    }
}
```

### 6. Startup Integration

#### Program.cs / Startup.cs Updates
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ... existing services ...
    
    // License services
    services.AddDbContext<LicenseDbContext>(options =>
        options.UseSqlServer(connectionString));
    
    services.AddScoped<IDeviceService, DeviceService>();
    services.AddScoped<ILicenseEncryptionService, LicenseEncryptionService>();
    services.AddScoped<IAccountStatusService, AccountStatusService>();
    services.AddScoped<ILicenseCacheService, LicenseCacheService>();
    services.AddScoped<ILicenseValidationService, LicenseValidationService>();
    services.AddHttpClient<ICloudLicenseService, CloudLicenseService>();
    services.AddScoped<ICloudLicenseService, CloudLicenseService>();
    
    // Background services
    services.AddHostedService<CloudSyncBackgroundService>();
    
    // ... other services ...
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ... existing middleware ...
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    // License middleware (after authentication)
    app.UseMiddleware<LicenseErrorHandlingMiddleware>();
    app.UseMiddleware<LicenseValidationMiddleware>();
    
    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

### 7. Configuration Updates

#### appsettings.json
```json
{
  "Licensing": {
    "CloudEndpoint": "https://license.example.com",
    "BearerToken": "your-secure-bearer-token",
    "EncryptionKey": "BASE64_ENCODED_32_BYTE_KEY",
    "SyncIntervalMinutes": 10,
    "ValidationIntervalHours": 24,
    "CacheExpirationHours": 24,
    "GracePeriodDays": 7,
    "DemoExpirationDays": 30,
    "MaxRetryAttempts": 24,
    "RetryIntervalHours": 1,
    "WarningThresholdDays": 3
  }
}
```

### 8. SessionInfo Extension

The existing SessionInfo class should be extended to include DeviceUuid:

```csharp
public class SessionInfo 
{
    // ... existing properties ...
    public string DeviceUuid { get; set; }
}
```

This will be populated from the X-Device-UUID header during authentication.

### 9. Testing Integration

#### License Integration Tests
```csharp
[TestFixture]
public class LicenseIntegrationTests
{
    private TestServer _server;
    private HttpClient _client;
    private LicenseDbContext _context;

    [SetUp]
    public void Setup()
    {
        var builder = new WebHostBuilder()
            .UseStartup<TestStartup>()
            .ConfigureServices(services =>
            {
                services.AddDbContext<LicenseDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });

        _server = new TestServer(builder);
        _client = _server.CreateClient();
        _context = _server.Services.GetRequiredService<LicenseDbContext>();
    }

    [Test]
    public async Task API_WithoutDeviceUuid_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/some-protected-endpoint");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Test]
    public async Task API_WithInactiveDevice_ReturnsForbidden()
    {
        // Setup inactive device
        var device = new Device
        {
            DeviceUuid = "inactive-device",
            Status = DeviceStatus.Inactive
        };
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Add("X-Device-UUID", "inactive-device");
        var response = await _client.GetAsync("/api/some-protected-endpoint");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Test]
    public async Task API_WithActiveDevice_ReturnsSuccess()
    {
        // Setup active device and account
        var device = new Device
        {
            DeviceUuid = "active-device",
            Status = DeviceStatus.Active
        };
        _context.Devices.Add(device);
        
        var account = new AccountStatus
        {
            Status = AccountState.Active,
            ExpirationDate = DateTime.UtcNow.AddDays(30)
        };
        _context.AccountStatuses.Add(account);
        await _context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Add("X-Device-UUID", "active-device");
        var response = await _client.GetAsync("/api/some-protected-endpoint");
        
        // Should not be forbidden due to license
        Assert.AreNotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Test]
    public async Task Authentication_WithLicenseWarning_ReturnsWarning()
    {
        // Setup account with payment due
        var account = new AccountStatus
        {
            Status = AccountState.PaymentDue,
            ExpirationDate = DateTime.UtcNow.AddDays(3)
        };
        _context.AccountStatuses.Add(account);
        await _context.SaveChangesAsync();

        var response = await _client.GetAsync("/api/authentication/companyname");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CompanyNameResponse>(content);
        
        Assert.IsTrue(result.LicenseWarnings.Any());
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _server?.Dispose();
    }
}
```

### 10. End-to-End Testing

#### E2E Test Scenarios
```csharp
[TestFixture]
public class LicenseE2ETests
{
    [Test]
    public async Task CompleteDeviceLicenseFlow_WorksCorrectly()
    {
        // 1. Register device
        var registerResponse = await RegisterDeviceAsync("test-device-001", "Test Device");
        Assert.IsTrue(registerResponse.IsSuccessful);

        // 2. Login and get license status
        var loginResponse = await LoginAsync("test-device-001");
        Assert.IsTrue(loginResponse.IsSuccessful);
        Assert.IsEmpty(loginResponse.LicenseWarnings);

        // 3. Make API calls with device
        var apiResponse = await MakeAuthorizedApiCallAsync("test-device-001");
        Assert.IsTrue(apiResponse.IsSuccessful);

        // 4. Simulate payment due
        await SimulatePaymentDueAsync();

        // 5. Login should show warning
        var loginWithWarning = await LoginAsync("test-device-001");
        Assert.IsTrue(loginWithWarning.IsSuccessful);
        Assert.IsNotEmpty(loginWithWarning.LicenseWarnings);

        // 6. API calls should still work
        var apiWithWarning = await MakeAuthorizedApiCallAsync("test-device-001");
        Assert.IsTrue(apiWithWarning.IsSuccessful);

        // 7. Simulate account disabled
        await SimulateAccountDisabledAsync();

        // 8. API calls should fail
        var apiDisabled = await MakeAuthorizedApiCallAsync("test-device-001");
        Assert.IsFalse(apiDisabled.IsSuccessful);
    }

    [Test]
    public async Task CloudSyncIntegration_WorksCorrectly()
    {
        // Start mock cloud server
        using var mockServer = StartMockCloudServer();
        
        // Register device - should queue cloud event
        await RegisterDeviceAsync("test-device-002", "Test Device 2");
        
        // Wait for background sync
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        // Verify event was processed
        var queueStatus = await GetQueueStatusAsync();
        Assert.AreEqual(0, queueStatus.PendingEventCount);
        
        // Verify device is registered in mock server
        var mockDevices = await GetMockServerDevicesAsync();
        Assert.IsTrue(mockDevices.Any(d => d.DeviceUuid == "test-device-002"));
    }
}
```

## Success Criteria

- [ ] License validation middleware enforces access control
- [ ] API endpoints properly restricted based on license status
- [ ] Authentication responses include license warnings
- [ ] Device UUID properly tracked and validated
- [ ] Error handling provides clear license-related messages
- [ ] Integration tests validate complete workflow
- [ ] E2E tests demonstrate full system functionality
- [ ] Performance impact is minimal
- [ ] System maintains backward compatibility

## Dependencies

### Phase 1, 2, 3 Components
- Device management system
- Account status management
- License caching and validation
- Cloud synchronization service

### Internal Dependencies
- Existing authentication system
- API controller infrastructure
- Middleware pipeline
- Configuration system

## Deliverables

1. License validation middleware
2. Enhanced authentication controller
3. License status API endpoints
4. Error handling middleware
5. Configuration updates
6. Integration tests
7. End-to-end test suite
8. Documentation updates
9. Performance benchmarks

## Final System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                          Frontend                               │
│                    (Device UUID in localStorage)               │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          │ HTTP Requests with X-Device-UUID header
                          │
┌─────────────────────────▼───────────────────────────────────────┐
│                    WMS Backend API                              │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                  Middleware Pipeline                        ││
│  │                                                             ││
│  │  Authentication → License Validation → Authorization        ││
│  │                                                             ││
│  │  • DeviceUuidMiddleware                                     ││
│  │  • LicenseValidationMiddleware                              ││
│  │  • LicenseErrorHandlingMiddleware                           ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                   API Controllers                           ││
│  │                                                             ││
│  │  • AuthenticationController (with license warnings)        ││
│  │  • DeviceController (superuser only)                       ││
│  │  • LicenseController (status and admin)                    ││
│  │  • Protected Controllers (require active device)           ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                 License Services                            ││
│  │                                                             ││
│  │  • DeviceService                                            ││
│  │  • AccountStatusService                                     ││
│  │  • LicenseCacheService                                      ││
│  │  • LicenseValidationService                                 ││
│  │  • CloudLicenseService                                      ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │              Background Services                            ││
│  │                                                             ││
│  │  • CloudSyncBackgroundService                               ││
│  │    - Process sync queue every 10 minutes                    ││
│  │    - Daily account validation                               ││
│  │    - Retry failed events hourly                             ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                License Database                             ││
│  │                                                             ││
│  │  • Devices (UUID, Status, Audit)                           ││
│  │  • AccountStatus (State, Expiration)                       ││
│  │  • LicenseCache (Encrypted Data)                           ││
│  │  • CloudSyncQueue (Retry Logic)                            ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          │ HTTPS with Bearer Token
                          │
┌─────────────────────────▼───────────────────────────────────────┐
│                Cloud License Server                            │
│                                                                 │
│  • Device event processing                                      │
│  • Account validation                                           │
│  • Payment status management                                    │
│  • IP-locked bearer token authentication                       │
└─────────────────────────────────────────────────────────────────┘
```

## Deployment Checklist

- [ ] Database migrations applied
- [ ] Configuration values set
- [ ] SSL certificates configured
- [ ] Cloud server endpoints configured
- [ ] Bearer tokens generated and secured
- [ ] Encryption keys generated and stored securely
- [ ] Background services configured
- [ ] Logging configured
- [ ] Monitoring alerts set up
- [ ] Testing completed in staging environment
- [ ] Documentation updated
- [ ] Training materials prepared

## Post-Deployment Tasks

1. **Monitor Initial Deployment**
   - Check background service logs
   - Verify cloud communication
   - Monitor API response times
   - Check database performance

2. **Validate License Flow**
   - Register test devices
   - Verify device activation
   - Test account status transitions
   - Validate access control

3. **Performance Optimization**
   - Monitor middleware performance
   - Optimize database queries
   - Tune cache expiration
   - Adjust sync intervals

4. **Security Review**
   - Verify encryption implementation
   - Check token security
   - Review access logs
   - Test IP locking

This completes the comprehensive licensing system implementation plan. The system provides robust device management, cloud synchronization, and access control while maintaining security and performance standards.