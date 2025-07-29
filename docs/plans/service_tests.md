# Service Configuration Testing Plans

## Overview
This document outlines the comprehensive testing framework for validating service configurations and connections in the EzyWMS service.

## Current Implementation

### SBO Connection Testing (✅ Implemented)
- **Command**: `./Service --test-sbo`
- **Purpose**: Test SAP Business One connectivity
- **Features**:
  - Display SboSettings configuration (password masked)
  - Test Service Layer connection
  - Show adapter type (Service Layer vs Windows COM)
  - Proper exit codes for CI/CD integration

## Planned Testing Extensions

### 1. Database Connection Testing
- **Command**: `./Service --test-database`
- **Purpose**: Validate database connectivity
- **Tests**:
  - Test DefaultConnection string
  - Test ExternalAdapterConnection string
  - Verify database schema compatibility
  - Test Entity Framework migrations status
  - Performance benchmarks for query execution

### 2. Redis/Session Management Testing
- **Command**: `./Service --test-session`
- **Purpose**: Validate session management configuration
- **Tests**:
  - Redis connectivity (if configured)
  - In-memory cache fallback
  - Session storage/retrieval operations
  - Session expiration handling

### 3. External System Integration Testing
- **Command**: `./Service --test-external`
- **Purpose**: Test external system connections
- **Tests**:
  - External commands configuration validation
  - File system access for external command destinations
  - Network connectivity to external endpoints
  - Command execution simulation

### 4. Licensing System Testing
- **Command**: `./Service --test-license`
- **Purpose**: Validate licensing configuration
- **Tests**:
  - Cloud endpoint connectivity
  - License validation with demo data
  - Device registration simulation
  - License cache functionality

### 5. Background Services Testing
- **Command**: `./Service --test-background`
- **Purpose**: Test background service configurations
- **Tests**:
  - PickList sync service configuration
  - Cloud sync service settings
  - Service scheduling validation
  - Performance impact assessment

### 6. Configuration Validation Suite
- **Command**: `./Service --test-config`
- **Purpose**: Comprehensive configuration validation
- **Tests**:
  - All appsettings.json sections
  - Required vs optional settings
  - Data type validations
  - Cross-configuration dependencies
  - Environment-specific overrides

### 7. Security Configuration Testing
- **Command**: `./Service --test-security`
- **Purpose**: Validate security settings
- **Tests**:
  - JWT configuration validation
  - Cookie security settings
  - CORS policy validation
  - Authentication/authorization setup
  - Password encryption validation

### 8. Performance Benchmark Testing
- **Command**: `./Service --test-performance`
- **Purpose**: Baseline performance testing
- **Tests**:
  - Configuration loading time
  - Database connection pool performance
  - Memory usage baseline
  - Service startup time analysis

## Implementation Architecture

### Tester Interface Pattern
```csharp
public interface IConfigurationTester {
    Task<TestResult> RunTestAsync();
    string TestName { get; }
    string Description { get; }
}

public class TestResult {
    public bool Success { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object> Details { get; set; }
    public TimeSpan Duration { get; set; }
}
```

### Test Runner Framework
```csharp
public class ConfigurationTestRunner {
    public async Task<TestSuite> RunTestSuiteAsync(string testType);
    public async Task<TestResult> RunSingleTestAsync(string testName);
    public void DisplayResults(TestSuite results);
}
```

### Planned Test Classes
- `DatabaseConnectionTester`
- `RedisSessionTester`
- `ExternalSystemTester`
- `LicensingTester`
- `BackgroundServiceTester`
- `SecurityConfigurationTester`
- `PerformanceBenchmarkTester`

## Output Formats

### Console Output (Default)
- Colored output for success/failure
- Detailed error messages
- Progress indicators for long-running tests

### JSON Output
- **Command**: `./Service --test-[type] --output=json`
- Machine-readable format for CI/CD integration
- Structured error details and metrics

### XML Output
- **Command**: `./Service --test-[type] --output=xml`
- Compatible with test result parsers
- Detailed test execution reports

## Integration with CI/CD

### Exit Codes
- `0`: All tests passed
- `1`: Configuration errors
- `2`: Connection failures
- `3`: Performance issues
- `4`: Security validation failures

### Environment Variables
- `EZYWMS_CONFIG_PATH`: Override default configuration path
- `EZYWMS_TEST_TIMEOUT`: Set test timeout in seconds
- `EZYWMS_TEST_VERBOSE`: Enable verbose logging

## Future Enhancements

### 1. Configuration Drift Detection
- Compare current configuration against baseline
- Detect unauthorized changes
- Alert on security configuration changes

### 2. Health Check Integration
- Expose test results via health check endpoints
- Continuous monitoring integration
- Automated alerting for configuration issues

### 3. Configuration Generation
- Generate optimal configurations based on environment
- Template-based configuration deployment
- Environment-specific validation rules

### 4. Test Automation
- Scheduled configuration validation
- Pre-deployment validation pipeline
- Post-deployment verification tests

## Usage Examples

```bash
# Test SAP connection
./Service --test-sbo

# Test all database connections
./Service --test-database

# Full configuration validation
./Service --test-config

# JSON output for CI/CD
./Service --test-sbo --output=json

# Verbose testing with timeout
EZYWMS_TEST_VERBOSE=true ./Service --test-external --timeout=60
```

## Notes

- All tests should run without starting the full web server
- Tests should be idempotent and safe to run in production environments
- Sensitive information (passwords, keys) should always be masked in output
- Each test should complete within reasonable time limits (< 30 seconds by default)
- Tests should provide actionable error messages and remediation suggestions