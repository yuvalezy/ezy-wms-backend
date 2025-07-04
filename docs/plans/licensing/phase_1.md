# Phase 1: Foundation & Device Management

## Objectives
- Establish database foundation for licensing system
- Implement device registration and management
- Create basic API endpoints for device operations
- Set up audit trail for device status changes

## Technical Tasks

### 1. Database Schema Implementation

#### Device Entity
```csharp
using Core.Entities;

public class Device : BaseEntity
{
    public string DeviceUuid { get; set; } // From frontend localStorage
    public string DeviceName { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DeviceStatus Status { get; set; }
    public string StatusNotes { get; set; }
    public DateTime LastActiveDate { get; set; }
}

public enum DeviceStatus
{
    Active = 1,
    Inactive = 2,
    Disabled = 3
}
```

#### Device Audit Entity
```csharp
using Core.Entities;

public class DeviceAudit : BaseEntity
{
    public Guid DeviceId { get; set; }
    public DeviceStatus PreviousStatus { get; set; }
    public DeviceStatus NewStatus { get; set; }
    public string Reason { get; set; }
    
    public virtual Device Device { get; set; }
}
```

### 2. License Database Context

#### LicenseDbContext
```csharp
public class LicenseDbContext : DbContext
{
    public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options) { }

    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceAudit> DeviceAudits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Device configuration
        modelBuilder.Entity<Device>(entity =>
        {
            entity.Property(e => e.DeviceUuid)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.DeviceName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.StatusNotes)
                .HasMaxLength(500);
            entity.HasIndex(e => e.DeviceUuid)
                .IsUnique();
        });

        // Device Audit configuration
        modelBuilder.Entity<DeviceAudit>(entity =>
        {
            entity.Property(e => e.Reason)
                .HasMaxLength(500);
            entity.HasOne(e => e.Device)
                .WithMany()
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

### 3. Device Service Implementation

#### IDeviceService Interface
```csharp
public interface IDeviceService
{
    Task<Device> RegisterDeviceAsync(string deviceUuid, string deviceName, SessionInfo sessionInfo);
    Task<Device> GetDeviceAsync(string deviceUuid);
    Task<List<Device>> GetAllDevicesAsync();
    Task<Device> UpdateDeviceStatusAsync(string deviceUuid, DeviceStatus status, string reason, SessionInfo sessionInfo);
    Task<Device> UpdateDeviceNameAsync(string deviceUuid, string newName, SessionInfo sessionInfo);
    Task<List<DeviceAudit>> GetDeviceAuditHistoryAsync(string deviceUuid);
    Task<bool> IsDeviceActiveAsync(string deviceUuid);
}
```

#### DeviceService Implementation
```csharp
public class DeviceService : IDeviceService
{
    private readonly LicenseDbContext _context;
    private readonly ILogger<DeviceService> _logger;

    public DeviceService(LicenseDbContext context, ILogger<DeviceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Device> RegisterDeviceAsync(string deviceUuid, string deviceName, SessionInfo sessionInfo)
    {
        // Check if device already exists
        var existingDevice = await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceUuid == deviceUuid);

        if (existingDevice != null)
        {
            throw new InvalidOperationException("Device already registered");
        }

        var device = new Device
        {
            DeviceUuid = deviceUuid,
            DeviceName = deviceName,
            RegistrationDate = DateTime.UtcNow,
            Status = DeviceStatus.Active,
            StatusNotes = "Initial registration",
            LastActiveDate = DateTime.UtcNow,
            CreatedByUserId = sessionInfo.Guid
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        // Log audit record
        await LogDeviceStatusChangeAsync(device.Id, DeviceStatus.Active, DeviceStatus.Active, 
            "Device registered", sessionInfo);

        _logger.LogInformation("Device {DeviceUuid} registered by user {UserId}", deviceUuid, sessionInfo.Guid);
        return device;
    }

    // Additional service methods...
}
```

### 4. Device Controller

#### DeviceController
```csharp
using Infrastructure.Auth;
using Service.Middlewares; // For HttpContext.GetSession() extension

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(IDeviceService deviceService, ILogger<DeviceController> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    [HttpPost("register")]
    [RequireSuperUser]
    public async Task<ActionResult<DeviceResponse>> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();
            var device = await _deviceService.RegisterDeviceAsync(
                request.DeviceUuid, request.DeviceName, sessionInfo);

            return Ok(new DeviceResponse
            {
                Id = device.Id,
                DeviceUuid = device.DeviceUuid,
                DeviceName = device.DeviceName,
                Status = device.Status.ToString(),
                RegistrationDate = device.RegistrationDate,
                StatusNotes = device.StatusNotes
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [RequireSuperUser]
    public async Task<ActionResult<List<DeviceResponse>>> GetAllDevices()
    {
        var devices = await _deviceService.GetAllDevicesAsync();
        var response = devices.Select(d => new DeviceResponse
        {
            Id = d.Id,
            DeviceUuid = d.DeviceUuid,
            DeviceName = d.DeviceName,
            Status = d.Status.ToString(),
            RegistrationDate = d.RegistrationDate,
            StatusNotes = d.StatusNotes,
            LastActiveDate = d.LastActiveDate
        }).ToList();

        return Ok(response);
    }

    [HttpPut("{deviceUuid}/status")]
    [RequireSuperUser]
    public async Task<ActionResult<DeviceResponse>> UpdateDeviceStatus(
        string deviceUuid, [FromBody] UpdateDeviceStatusRequest request)
    {
        try
        {
            var sessionInfo = HttpContext.GetSession();
            var status = Enum.Parse<DeviceStatus>(request.Status);
            var device = await _deviceService.UpdateDeviceStatusAsync(
                deviceUuid, status, request.Reason, sessionInfo);

            return Ok(new DeviceResponse
            {
                Id = device.Id,
                DeviceUuid = device.DeviceUuid,
                DeviceName = device.DeviceName,
                Status = device.Status.ToString(),
                RegistrationDate = device.RegistrationDate,
                StatusNotes = device.StatusNotes
            });
        }
        catch (ArgumentException)
        {
            return BadRequest(new { error = "Invalid device status" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{deviceUuid}/audit")]
    [RequireSuperUser]
    public async Task<ActionResult<List<DeviceAuditResponse>>> GetDeviceAuditHistory(string deviceUuid)
    {
        var auditHistory = await _deviceService.GetDeviceAuditHistoryAsync(deviceUuid);
        var response = auditHistory.Select(a => new DeviceAuditResponse
        {
            PreviousStatus = a.PreviousStatus.ToString(),
            NewStatus = a.NewStatus.ToString(),
            Reason = a.Reason,
            ChangedAt = a.ChangedAt
        }).ToList();

        return Ok(response);
    }

}
```

### 5. DTOs (Data Transfer Objects)

#### Request Models
```csharp
public class RegisterDeviceRequest
{
    public string DeviceUuid { get; set; }
    public string DeviceName { get; set; }
}

public class UpdateDeviceStatusRequest
{
    public string Status { get; set; }
    public string Reason { get; set; }
}
```

#### Response Models
```csharp
public class DeviceResponse
{
    public Guid Id { get; set; }
    public string DeviceUuid { get; set; }
    public string DeviceName { get; set; }
    public string Status { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string StatusNotes { get; set; }
    public DateTime? LastActiveDate { get; set; }
}

public class DeviceAuditResponse
{
    public string PreviousStatus { get; set; }
    public string NewStatus { get; set; }
    public string Reason { get; set; }
    public DateTime ChangedAt { get; set; }
}
```

## Database Schema Notes

When ready for database migration, the following tables will need to be created:

1. **Devices Table**
   - Inherits from BaseEntity (Guid Id, audit fields)
   - DeviceUuid (unique index)
   - DeviceName, Status, StatusNotes
   - RegistrationDate, LastActiveDate

2. **DeviceAudits Table**
   - Inherits from BaseEntity
   - Foreign key to Devices
   - Tracks status changes with reason

## Configuration Updates

### Startup.cs / Program.cs
```csharp
// Add to ConfigureServices
services.AddDbContext<LicenseDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IDeviceService, DeviceService>();
```

## Testing Approach

### Unit Tests
```csharp
[Test]
public async Task RegisterDevice_ValidData_ReturnsDevice()
{
    // Arrange
    var options = new DbContextOptionsBuilder<LicenseDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDb")
        .Options;

    using var context = new LicenseDbContext(options);
    var logger = new Mock<ILogger<DeviceService>>();
    var service = new DeviceService(context, logger.Object);

    // Act
    var result = await service.RegisterDeviceAsync("test-uuid", "Test Device", 1);

    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual("test-uuid", result.DeviceUuid);
    Assert.AreEqual(DeviceStatus.Active, result.Status);
}

[Test]
public async Task RegisterDevice_DuplicateUuid_ThrowsException()
{
    // Test duplicate registration
}

[Test]
public async Task UpdateDeviceStatus_ValidTransition_UpdatesStatus()
{
    // Test status updates
}
```

### Integration Tests
```csharp
[Test]
public async Task DeviceController_RegisterDevice_SuperUserOnly()
{
    // Test superuser authorization
}

[Test]
public async Task DeviceController_GetAllDevices_ReturnsDeviceList()
{
    // Test device listing
}
```

## Success Criteria

- [ ] Database schema created and migrations applied
- [ ] Device registration works correctly
- [ ] Device status updates function properly
- [ ] Audit trail captures all changes
- [ ] API endpoints protected with superuser authorization
- [ ] All unit tests pass
- [ ] Integration tests validate controller behavior

## Dependencies

### Internal
- Existing authentication system (JWT claims)
- Database connection configuration
- Authorization middleware

### External
- None for this phase

## Deliverables

1. Database entities and DbContext
2. Device service implementation
3. Device controller with API endpoints
4. Database migration files
5. Unit and integration tests
6. API documentation (Swagger)

## Next Phase

Phase 2 will build upon this foundation to add account status management and license caching functionality.