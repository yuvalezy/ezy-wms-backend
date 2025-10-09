# EzyWMS Service

A comprehensive ASP.NET Core Web API service for warehouse management integrated with SAP Business One.

## Overview

EzyWMS Service is a modular warehouse management system providing RESTful APIs for:
- Goods Receipt operations
- Transfer management
- Inventory counting
- Picking and order fulfillment

The service integrates seamlessly with SAP Business One and supports both SQL Server and SAP HANA databases.

## Architecture

### Solution Structure

- **Service** - Main ASP.NET Core Web API application (can run as Windows Service)
- **Service.Administration** - Windows Forms admin UI for configuration
- **Service.Shared** - Common components, data access, and business logic
- **Service.UnitTest** - Unit tests
- **MetaData** - Database metadata export/import utility

### Technology Stack

- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core
- JWT Authentication
- Redis (Session Management)
- SQL Server / SAP HANA
- Swagger/OpenAPI
- Windows Services support

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- SQL Server (or SAP HANA)
- Redis server (for session management)
- Visual Studio 2022 or JetBrains Rider (optional)
- SAP Business One (for SAP integration features)

### Installation

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd lw_server
   ```

2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

3. Configure the application settings:
   ```bash
   cd Service
   cp appsettings.example.json appsettings.Development.json
   ```

4. Update `appsettings.Development.json` with your settings:
   - Database connection strings
   - Redis configuration
   - JWT settings
   - CORS origins

### Configuration

Key configuration sections in `appsettings.json`:

#### Connection Strings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EZY_WMS;User Id=sa;Password=xxx;TrustServerCertificate=true;",
    "ExternalAdapterConnection": "Server=localhost;Database=SAP_DB;User Id=sa;Password=xxx;TrustServerCertificate=true;"
  }
}
```

#### JWT Authentication
```json
{
  "Jwt": {
    "Key": "<your-secret-key>",
    "Issuer": "EzyWMS",
    "Audience": "EzyWMS-Client",
    "ExpiresInMinutes": 60
  }
}
```

#### Redis Session Management
```json
{
  "SessionManagement": {
    "Type": "Redis",
    "Redis": {
      "Host": "localhost",
      "Port": 6379
    }
  }
}
```

### Running the Application

#### Development Mode
```bash
cd Service
dotnet run
```

The API will be available at `http://localhost:5000`

#### Debug in Visual Studio
1. Open `service.sln`
2. Set `Service` as the startup project
3. Press F5 to run

#### As Windows Service
```bash
dotnet publish -c Release
sc create EzyWMS binPath="<path-to-published-exe>"
sc start EzyWMS
```

## API Documentation

Once running, access the Swagger UI at:
```
http://localhost:5000/swagger
```

### API Module Pattern

Each module follows a consistent architecture:

1. **Controller** (`[Module]Controller.cs`) - HTTP endpoints
2. **Data** (`[Module]Data.cs`) - Business logic and queries
3. **Creation** (`[Module]Creation.cs`) - SAP document creation
4. **Models/** - DTOs and enumerations
5. **Queries/SQL/** - SQL queries as embedded resources

### Available Modules

- **Authentication** - Login, password management
- **Counting** - Inventory counting operations
- **GoodsReceipt** - Receiving operations
- **Transfer** - Transfer management
- **Picking** - Order picking and fulfillment

## Authentication

The service uses a hybrid authentication system:

1. **Login** via `POST /api/authentication/login`
   - Password-only authentication
   - Returns JWT token and session info
   - Sets HTTP-only cookie for browser clients

2. **Authorization**
   - Use Bearer token in Authorization header, OR
   - Cookie-based session (automatic for browsers)
   - Token expires at midnight UTC

Example login:
```bash
curl -X POST http://localhost:5000/api/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"password":"your-password"}'
```

## Database Support

The service supports multiple database platforms:

- **SQL Server** - Primary database
- **SAP HANA** - For SAP B1 integration

SQL queries are organized in platform-specific folders:
- `Queries/SQL/` - SQL Server queries
- `Queries/HANA/` - SAP HANA queries

## Development

### Adding a New API Endpoint

1. Create controller method in `Service/API/[Module]/[Module]Controller.cs`
2. Implement business logic in `[Module]Data.cs`
3. Create SQL query in `Queries/SQL/YourQuery.sql`
4. Add query as EmbeddedResource in `Service.csproj`
5. Create request/response models in `Models/` folder
6. Add `[Authorize]` attribute if authentication is required

### Code Guidelines

- Use primary constructors
- Declare namespaces as `namespace MyNamespace;` (file-scoped)
- Follow SOLID principles
- Keep files under 500 lines (refactor if larger)
- Never change enum number values
- Include `TODO` at the beginning of placeholder comments

### Embedded SQL Queries

Load queries using:
```csharp
string query = GetQuery("QueryName"); // Auto-selects SQL/HANA variant
```

## Testing

Run unit tests:
```bash
dotnet test
```

## Deployment

### Building for Production
```bash
dotnet publish -c Release -o ./publish
```

### Run Modes
- **ASP.NET Core Web API** - Default Kestrel mode
- **Windows Service** - Background service on Windows
- **Interactive** - Console mode for debugging
- **Background** - Background processing only
- **Load Balanced** - Multiple nodes with Redis coordination

## Security Features

- HTTP-only cookies prevent XSS attacks
- JWT token validation
- PBKDF2 password hashing (SHA256, 10,000 iterations)
- CORS configuration
- Secure session management with Redis

## Background Services

The service includes background workers:

### PickList Sync
- Interval: 30 seconds (configurable)
- Syncs pick lists with SAP B1
- Processes package movements

### Cloud Sync
- Sync interval: 10 minutes (configurable)
- Validation interval: 24 hours
- Synchronizes with cloud services

Configure in `appsettings.json`:
```json
{
  "BackgroundServices": {
    "PickListSync": {
      "IntervalSeconds": 30,
      "Enabled": true
    }
  }
}
```

## Licensing

The service includes a licensing system with:
- Cloud-based validation
- Grace period support (7 days default)
- Demo mode (30 days default)
- Cached license validation (24 hours)

## Troubleshooting

### Common Issues

**Port already in use**
```bash
# Change port in appsettings.json
"Kestrel": {
  "Endpoints": {
    "Http": { "Url": "http://0.0.0.0:5001" }
  }
}
```

**Database connection failed**
- Verify connection string in `appsettings.json`
- Ensure SQL Server is running
- Check firewall settings

**Redis connection failed**
- Verify Redis is running: `redis-cli ping`
- Check Redis host/port in configuration
- Ensure Redis accepts remote connections

**SAP B1 integration issues**
- Verify SAP Business One is installed
- Check SAP credentials and permissions
- Ensure COM interop is properly configured

## Administration

Use the **Service.Administration** Windows Forms app to:
- Configure service settings
- Manage database connections
- View logs and diagnostics
- Test API connections

## Contributing

1. Follow the existing code patterns
2. Add unit tests for new features
3. Update documentation
4. Keep commits focused and atomic
5. Follow SOLID principles

## Support

For issues and questions:
- Check the documentation
- Review existing issues
- Contact the development team

## License

[Add your license information here]

## Version History

See [CHANGELOG.md](CHANGELOG.md) for version history and release notes.
