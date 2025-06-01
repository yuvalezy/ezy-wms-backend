# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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