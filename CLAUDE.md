# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

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

### Authentication System

The application uses a hybrid authentication approach that combines JWT tokens with HTTP-only session cookies:

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

### Security Features
- HTTP-only cookies prevent XSS attacks
- Secure flag should be enabled in production (currently false for development)
- Tokens expire at midnight UTC
- Session data stored in memory for performance

## Common Development Tasks

### Adding a New API Endpoint
1. Add method to appropriate controller in `Service/API/[Module]/`
2. Add corresponding method in `[Module]Data.cs`
3. Create SQL query in `Queries/SQL/` (and HANA variant if needed)
4. Add query as EmbeddedResource in Service.csproj
5. Create request/response models in `Models/` folder
6. Add `[Authorize]` attribute if authentication is required

### Running in Debug Mode
**Note**: The Service project only runs on Windows due to SAP Business One COM interop dependencies.
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