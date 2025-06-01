# .NET Framework 4.8 to .NET 9 Migration Guide

## Overview
This guide documents the migration process from .NET Framework 4.8 to .NET 9 for the LW Service solution.

## Migration Status

### Completed Steps
1. ✅ Created backup files (.backup extension)
2. ✅ Created new SDK-style project files (.new extension)
3. ✅ Created new solution file (service.sln.new)
4. ✅ Created Program.cs for ASP.NET Core
5. ✅ Created appsettings.json for configuration

### Migration Challenges and Solutions

#### 1. SAP Business One COM Interop
**Challenge**: COM interop is Windows-only and not fully supported in .NET Core on Linux.

**Solution**: 
- Target `net9.0-windows` framework
- Keep COM references for Windows deployment
- Consider creating a separate microservice for SAP operations
- Alternative: Use SAP Business One Service Layer REST API

#### 2. Crystal Reports
**Challenge**: Limited .NET Core support for Crystal Reports.

**Solution**:
- Use CrystalReports.Engine.NetCore and CrystalReports.Shared.NetCore packages
- Consider alternatives: SSRS, FastReport, or other reporting solutions
- Keep Crystal Reports in a separate Windows-only service if needed

#### 3. Windows Service
**Challenge**: Converting from Windows Service to cross-platform service.

**Solution**:
- Use `Microsoft.Extensions.Hosting.WindowsServices` package
- Implement as ASP.NET Core app that can run as Windows Service
- Use `UseWindowsService()` in Program.cs

#### 4. OWIN to ASP.NET Core
**Challenge**: Migrating from OWIN self-host to ASP.NET Core.

**Solution**:
- Replace OWIN middleware with ASP.NET Core middleware
- Use Kestrel as the web server
- Update authentication to ASP.NET Core Identity/JWT

#### 5. System.Web Dependencies
**Challenge**: System.Web is not available in .NET Core.

**Solution**:
- Replace HttpContext with IHttpContextAccessor
- Use ASP.NET Core equivalents for utilities
- Update authentication/authorization code

## Next Steps

### 1. Apply New Project Files
```bash
# Replace old files with new ones
mv Service.Shared/Service.Shared.csproj.new Service.Shared/Service.Shared.csproj
mv Service/Service.csproj.new Service/Service.csproj
mv service.sln.new service.sln
mv Service/Program.cs.new Service/Program.cs
```

### 2. Update Code Files
- Replace System.Web references with ASP.NET Core equivalents
- Update controllers to inherit from ControllerBase
- Convert OWIN Startup.cs to ASP.NET Core Startup
- Update authentication code

### 3. Handle Platform-Specific Code
- Wrap COM interop calls in Windows-only checks
- Create abstractions for platform-specific functionality
- Consider dependency injection for different implementations

### 4. Update Dependencies
- Find .NET Core compatible versions of all packages
- Replace incompatible packages with alternatives
- Update NuGet package references

### 5. Testing
- Update unit tests to use modern test frameworks
- Test Windows Service functionality
- Test API endpoints
- Verify SAP integration

## Migration Commands

```bash
# Build the solution
dotnet build

# Run the service
dotnet run --project Service/Service.csproj

# Install as Windows Service
sc create "LWService" binPath="C:\path\to\Service.exe"

# Publish for deployment
dotnet publish -c Release -r win-x64 --self-contained
```

## Configuration Migration

### From App.config to appsettings.json
- Connection strings: Move to `ConnectionStrings` section
- App settings: Move to custom configuration sections
- Use IConfiguration instead of ConfigurationManager

### Environment-Specific Settings
- Use appsettings.Development.json
- Use appsettings.Production.json
- Use environment variables

## Deployment Options

### 1. Windows Service (Recommended for SAP integration)
- Deploy as Windows Service on Windows Server
- Maintains full SAP COM interop support
- Use existing deployment infrastructure

### 2. Docker Container (For non-SAP components)
- Create Dockerfile for containerized deployment
- Use multi-stage builds
- Deploy to Kubernetes or Docker Swarm

### 3. Hybrid Approach
- Core API in Docker/Linux
- SAP integration service on Windows
- Communication via REST API or message queue

## Rollback Plan
All original files are preserved with .backup extension. To rollback:
```bash
mv Service.Shared/Service.Shared.csproj.backup Service.Shared/Service.Shared.csproj
mv Service/Service.csproj.backup Service/Service.csproj
mv service.sln.backup service.sln
rm Service/Program.cs.new Service/appsettings.json
```