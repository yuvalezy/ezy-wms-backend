# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Coding Style Guidelines
# Code Formatting Style Guide

## C# Formatting Rules

### Constructor Parameters
- Align constructor parameters vertically with consistent spacing
- Add spaces to align parameter names in columns

**Format:**
```csharp
public class AuthenticationService(
    SystemDbContext                dbContext,
    IJwtAuthenticationService      jwtService,
    ISessionManager                sessionManager,
    ILogger<AuthenticationService> logger) : IAuthenticationService {
```

### Braces and Spacing
- Opening braces `{` on same line as declaration
- No empty lines after opening braces in try blocks
- Consistent brace placement for all code blocks

**Format:**
```csharp
public async Task<SessionInfo?> LoginAsync(string password) {
    try {
        // code here
    }
}
```

### General Rules
- Keep using statements as-is (no formatting changes needed)
- Maintain namespace declaration format
- Apply consistent spacing and alignment throughout all C# code
- Always use same-line brace style for classes, methods, and control structures

## Apply These Rules
- When generating C# code, always use this formatting style
- When updating existing code, convert to this format
- Maintain consistency across all code artifacts

### SOLID Principles (REQUIRED)
All new code MUST follow SOLID principles:

1. **Single Responsibility Principle (SRP)**
   - Each class should have only one reason to change
   - Controllers should only handle HTTP concerns
   - Business logic belongs in service classes
   - Data access belongs in repository classes

2. **Open/Closed Principle (OCP)**
   - Classes should be open for extension but closed for modification
   - Use interfaces and dependency injection
   - Prefer composition over inheritance

3. **Liskov Substitution Principle (LSP)**
   - Derived classes must be substitutable for their base classes
   - Interface implementations must fulfill the contract completely

4. **Interface Segregation Principle (ISP)**
   - Clients should not be forced to depend on interfaces they don't use
   - Keep interfaces small and focused

5. **Dependency Inversion Principle (DIP)**
   - Depend on abstractions (interfaces), not concretions
   - High-level modules should not depend on low-level modules
   - Both should depend on abstractions

### Implementation Pattern
```csharp
// Interface in Core project
public interface IUserService {
    Task<UserResponse> GetUserAsync(Guid id);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request);
}

// Implementation in Infrastructure project
public class UserService(SystemDbContext dbContext, ILogger<UserService> logger) : IUserService {
    public async Task<UserResponse> GetUserAsync(Guid id) {
        // Implementation
    }
}

// Controller uses interface
public class UserController(IUserService userService) : ControllerBase {
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id) {
        var user = await userService.GetUserAsync(id);
        return Ok(user);
    }
}
```

### Primary Constructors
Always use C# 12 primary constructors for classes when possible. This provides cleaner, more concise code.

```csharp
// Preferred
public class MyService(IConfiguration configuration, ILogger<MyService> logger) : IMyService {
    private readonly string setting = configuration["MySetting"] ?? "default";
    
    public void DoWork() {
        logger.LogInformation("Working...");
    }
}

// Avoid
public class MyService : IMyService {
    private readonly IConfiguration _configuration;
    private readonly ILogger<MyService> _logger;
    
    public MyService(IConfiguration configuration, ILogger<MyService> logger) {
        _configuration = configuration;
        _logger = logger;
    }
}
```

## Build Commands

This is a .NET 9 solution that uses the dotnet CLI. The solution has three build configurations: Debug, Release, and Test.

```bash
# Build the entire solution
dotnet build service.sln -c Debug
dotnet build service.sln -c Release

# Build specific projects
dotnet build Service/Service.csproj -c Debug
dotnet build Service.Administration/Service.Administration.csproj -c Debug

# Run the service
dotnet run --project Service/Service.csproj

# Run as Windows Service
sc create "LWService" binPath="C:\path\to\Service.exe"

# Run unit tests
dotnet test Service.UnitTest/Service.UnitTest.csproj

# Publish for deployment
dotnet publish Service/Service.csproj -c Release -r win-x64 --self-contained
```

## Architecture Overview

### Solution Structure
- **Service** - Main ASP.NET Core Web API application hosting the REST endpoints (can run as Windows Service)
- **Service.Administration** - Windows Forms administration UI for service configuration
- **Service.Shared** - Common components, data access, and business logic
- **Service.UnitTest** - Unit tests for the service
- **MetaData** - Utility for exporting/importing database metadata

### API Module Pattern
Each API module (Counting, GoodsReceipt, Transfer, Picking) follows this consistent architecture:

1. **Controller** (`[Module]Controller.cs`) - HTTP endpoints, inherits from `LWApiController` (now `ControllerBase`)
2. **Data** (`[Module]Data.cs`) - Business logic and database queries
3. **Creation** (`[Module]Creation.cs`) - SAP Business One document creation
4. **Models/** - Request/response DTOs and enumerations
5. **Queries/SQL/** - Embedded SQL queries as resources

### Key Classes and Patterns

**Base Controller**: All API controllers inherit from `LWApiController` which provides:
- Inherits from ASP.NET Core `ControllerBase`
- `Data` property for accessing all module data classes
- `EmployeeID` property from JWT claims
- Common authorization checks

**Dependency Injection**: Services are registered in Program.cs:
```csharp
services.AddSingleton<IGlobalService, GlobalService>();
services.AddSingleton<IJwtAuthenticationService, JwtAuthenticationService>();
```

**Transaction Pattern**:
```csharp
using var conn = Global.Connector;
conn.BeginTransaction();
try {
    // Operations
    conn.CommitTransaction();
} catch {
    conn.RollbackTransaction();
    throw;
}
```

**Query Loading**: SQL queries are embedded resources loaded via:
```csharp
GetQuery("QueryName") // Automatically selects SQL/HANA variant
```

### Database Support
The system supports both SQL Server and SAP HANA databases. Queries are stored in separate folders:
- `Queries/SQL/` - SQL Server queries
- `Queries/HANA/` - SAP HANA queries

### Service Modes
The service can run in multiple modes:
- **ASP.NET Core Web API** - Default mode with Kestrel
- **Windows Service** - Using Microsoft.Extensions.Hosting.WindowsServices
- **Interactive** - Console mode for debugging
- **Background** - Background processing mode
- **Load Balanced** - Multiple nodes with Redis coordination

### Authentication
Uses JWT Bearer tokens with ASP.NET Core authentication middleware. The `JwtAuthenticationService` handles token generation and validation.

## Common Development Tasks

### Adding a New API Endpoint
1. Add method to appropriate controller in `Service/API/[Module]/`
2. Add corresponding method in `[Module]Data.cs`
3. Create SQL query in `Queries/SQL/` (and HANA variant if needed)
4. Add query as EmbeddedResource in Service.csproj
5. Create request/response models in `Models/` folder
6. Add `[Authorize]` attribute if authentication is required

### Running in Debug Mode
1. Set Service project as startup project
2. Run with `dotnet run` or F5 in Visual Studio
3. API will be available at http://localhost:5000
4. Swagger UI available at http://localhost:5000/swagger

### Working with SAP Business One
- The service integrates with SAP B1 via COM interop (`Interop.SAPbobsCOM.dll`)
- Creation classes handle SAP document creation with proper transaction management
- Always use mutex locks when creating SAP documents to prevent conflicts
- **Note**: COM interop requires Windows platform (net9.0-windows target)

### Configuration
- Main config in `appsettings.json` and environment-specific files
- API settings managed through Service.Administration UI
- Database connections can be stored in configuration or registry
- JWT settings in `Jwt` section of appsettings.json

## Migration from .NET Framework 4.8
This solution has been migrated from .NET Framework 4.8 to .NET 9. Key changes:
- OWIN replaced with ASP.NET Core
- System.Web replaced with ASP.NET Core equivalents
- App.config replaced with appsettings.json
- Windows Service support via hosted services
- JWT authentication instead of OAuth2

## Authentication System

The application uses a hybrid authentication approach that combines JWT tokens with HTTP-only session cookies:

### Authentication Flow
1. **Login** (`POST /api/authentication/login`)
   - Accepts password-only authentication (no username required)
   - Generates JWT token with user claims
   - Stores SessionInfo in memory via ISessionManager using token as key
   - Sets HTTP-only cookie with the token for session management
   - Returns SessionInfo with token for API clients

2. **Session Management**
   - JWT token is stored in HTTP-only cookie (`ezywms_session`)
   - SessionInfo is cached in memory for fast access
   - TokenSessionMiddleware validates sessions from cookies
   - Supports both cookie-based sessions and Bearer token authentication

3. **Password Management**
   - Passwords are hashed using PBKDF2 with SHA256
   - 32-byte salt + 10,000 iterations
   - Change password endpoint requires current password verification

### Using the API
- **Web Applications**: Automatically use HTTP-only cookies after login
- **API Clients (Postman, etc.)**: Use Bearer token from login response
  ```
  Authorization: Bearer <token>
  ```

### Security Features
- HTTP-only cookies prevent XSS attacks
- Secure flag should be enabled in production (currently false for development)
- Tokens expire at midnight UTC
- Session data stored in memory for performance

## Database Patterns

### Soft Delete Policy
**IMPORTANT**: We NEVER physically delete records from the database. All entities that inherit from `BaseEntity` use soft deletes.

#### Implementation
```csharp
// BaseEntity includes:
public bool Deleted { get; set; }
public DateTime? DeletedAt { get; set; }

// When "deleting" an entity:
entity.Deleted = true;
entity.DeletedAt = DateTime.UtcNow;
await dbContext.SaveChangesAsync();
```

#### Query Patterns
When querying data, always filter out soft-deleted records unless specifically including them:
```csharp
// Get active (non-deleted) users
var activeUsers = await dbContext.Users
    .Where(u => !u.Deleted)
    .ToListAsync();

// Include deleted records when needed
var allUsersIncludingDeleted = await dbContext.Users
    .ToListAsync();
```

#### Best Practices
1. All delete operations should set `Deleted = true` and `DeletedAt = DateTime.UtcNow`
2. Consider also setting `Active = false` for entities with an Active flag
3. Update any related business logic (e.g., prevent login for deleted users)
4. Use global query filters in DbContext for automatic filtering of deleted records
5. Deleted records should be excluded from all normal business operations