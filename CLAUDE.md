# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

This is a .NET Framework 4.8 solution that uses MSBuild. The solution has three build configurations: Debug, Release, and Test.

```bash
# Build the entire solution
msbuild service.sln /p:Configuration=Debug /p:Platform="Any CPU"
msbuild service.sln /p:Configuration=Release /p:Platform="Any CPU"

# Build specific projects
msbuild Service/Service.csproj /p:Configuration=Debug /p:Platform=x64
msbuild Service.Administration/Service.Administration.csproj /p:Configuration=Debug /p:Platform=x64

# Run the service in interactive mode
cd Service/bin/x64/Debug
./Service.exe

# Run unit tests (if MSTest is installed)
mstest /testcontainer:Service.UnitTest/bin/Debug/Service.UnitTest.dll
```

## Architecture Overview

### Solution Structure
- **Service** - Main Windows Service/Web API application hosting the REST endpoints
- **Service.Administration** - Windows Forms administration UI for service configuration
- **Service.Shared** - Common components, data access, and business logic
- **Service.UnitTest** - Unit tests for the service
- **MetaData** - Utility for exporting/importing database metadata

### API Module Pattern
Each API module (Counting, GoodsReceipt, Transfer, Picking) follows this consistent architecture:

1. **Controller** (`[Module]Controller.cs`) - HTTP endpoints, inherits from `LWApiController`
2. **Data** (`[Module]Data.cs`) - Business logic and database queries
3. **Creation** (`[Module]Creation.cs`) - SAP Business One document creation
4. **Models/** - Request/response DTOs and enumerations
5. **Queries/SQL/** - Embedded SQL queries as resources

### Key Classes and Patterns

**Base Controller**: All API controllers inherit from `LWApiController` which provides:
- `Data` property for accessing all module data classes
- `EmployeeID` property from authorization
- Common authorization checks

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
- **Windows Service** - Normal production mode
- **Interactive** - Console mode for debugging (`Service.exe`)
- **Background** - Background processing mode
- **Load Balanced** - Multiple nodes with Redis coordination

### Authentication
Uses OAuth2 with bearer tokens. The `ApplicationAuthProvider` handles authentication against SAP Business One users.

## Common Development Tasks

### Adding a New API Endpoint
1. Add method to appropriate controller in `Service/API/[Module]/`
2. Add corresponding method in `[Module]Data.cs`
3. Create SQL query in `Queries/SQL/` (and HANA variant if needed)
4. Add query as EmbeddedResource in Service.csproj
5. Create request/response models in `Models/` folder

### Running in Debug Mode
1. Set Service project as startup project
2. Run in Debug mode - it will start in interactive console mode
3. Press R for Hello World test, L to reload settings, A to reload API settings

### Working with SAP Business One
- The service integrates with SAP B1 via COM interop (`Interop.SAPbobsCOM.dll`)
- Creation classes handle SAP document creation with proper transaction management
- Always use mutex locks when creating SAP documents to prevent conflicts

### Configuration
- Main config in `App.config` and runtime settings in `Settings/` folder
- API settings managed through Service.Administration UI
- Database connections stored encrypted in registry