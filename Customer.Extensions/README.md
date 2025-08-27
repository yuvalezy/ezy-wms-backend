# Customer Post-Processing Extensions

This project demonstrates how to implement custom post-processing logic that executes after PickingUpdate operations.

## Overview

The post-processing system allows customers to add custom business logic that runs after successful picking operations without modifying the core application code.

## Architecture

- **IPickingPostProcessor**: Interface that all processors must implement
- **PickingPostProcessorContext**: Provides access to processed data, services, and configuration
- **Plugin Loading**: Processors are loaded from external DLLs and managed through dependency injection

## Implementation Example

The `PackagingCalculatorPostProcessor` demonstrates your specific requirement:

1. **Query EF Database**: Retrieves picking data with package barcodes from the internal EF database
2. **Execute Complex Logic**: Runs packaging calculations against the external SAP database using CTEs
3. **Update Orders**: Uses SAP Service Layer to update Order lines with SerialNum and PackQty fields

## Configuration

Add processor configuration to `appsettings.json`:

```json
{
  "PickingPostProcessing": {
    "Processors": [
      {
        "Id": "customer-packaging-calculator",
        "Assembly": "Customer.Extensions.dll",
        "TypeName": "Customer.Extensions.PackagingCalculatorPostProcessor", 
        "Enabled": true,
        "Configuration": {
          "Enabled": true,
          "BatchSize": 100,
          "TimeoutSeconds": 30
        }
      }
    ]
  }
}
```

## Deployment

1. **Build Extension**: Compile your extension project to produce `Customer.Extensions.dll`
2. **Deploy DLL**: Copy the DLL to the main application's directory (alongside `Service.exe`)
3. **Update Configuration**: Add processor configuration to `appsettings.json`
4. **Restart Service**: The service will automatically load and register the processors on startup

## Development Guidelines

### Processor Implementation

```csharp
public class MyCustomProcessor : IPickingPostProcessor 
{
    public string Id => "my-custom-processor";

    public async Task ExecuteAsync(PickingPostProcessorContext context, CancellationToken cancellationToken)
    {
        // Access processed pick list data
        var pickData = context.ProcessedData;
        var absEntry = context.AbsEntry;
        var config = context.Configuration;
        var logger = context.Logger;

        // Get services from DI container
        using var scope = context.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var sboCompany = scope.ServiceProvider.GetRequiredService<SboCompany>();

        // Implement your custom logic here
    }

    public bool IsEnabled(Dictionary<string, object>? configuration)
    {
        return configuration?.ContainsKey("Enabled") == true && 
               (bool)(configuration["Enabled"] ?? false);
    }
}
```

### Available Services

Through `context.ServiceProvider`, you can access:

- **SystemDbContext**: EF database context for internal data
- **SboCompany**: SAP Service Layer client for API calls
- **SboDatabaseService**: Direct SQL access to SAP database
- **ILogger**: Structured logging
- **Any other registered service**: All application services are available

### Error Handling

- **Individual Failures**: If one processor fails, others continue executing
- **Non-Breaking**: Post-processor failures don't affect the main picking operation
- **Comprehensive Logging**: All operations are logged with structured data

### Performance Considerations

- **Async Operations**: All operations are async to avoid blocking
- **Database Connections**: Use proper scoping and disposal
- **Batch Processing**: Consider batching for large data sets
- **Timeouts**: Implement reasonable timeouts for external calls

## Testing

Create unit tests for your processors:

```csharp
[Test]
public async Task PackagingCalculator_Should_UpdateOrders()
{
    // Arrange
    var context = new PickingPostProcessorContext
    {
        AbsEntry = 276,
        ProcessedData = GetTestPickLists(),
        Configuration = GetTestConfig(),
        Logger = Mock.Of<ILogger>(),
        ServiceProvider = GetTestServiceProvider()
    };

    var processor = new PackagingCalculatorPostProcessor();

    // Act
    await processor.ExecuteAsync(context);

    // Assert
    // Verify expected behavior
}
```

## Troubleshooting

### Common Issues

1. **Assembly Not Found**: Ensure DLL is in the correct path (same directory as Service.exe)
2. **Type Not Found**: Verify TypeName matches exactly (including namespace)
3. **Configuration Errors**: Check JSON syntax and processor enablement
4. **Service Dependencies**: Ensure all required services are registered

### Logging

All post-processor activities are logged with these patterns:

- `Executing post-processor {ProcessorId} for pick list {AbsEntry}`
- `Successfully executed post-processor {ProcessorId} for pick list {AbsEntry}`
- `Failed to execute post-processor {ProcessorId} for pick list {AbsEntry}`

### Performance Monitoring

Monitor execution times and consider implementing:

- Configuration-based timeouts
- Circuit breaker patterns for external calls
- Batch size limits
- Resource usage monitoring

## Advanced Scenarios

### Multiple Processors

You can register multiple processors that will execute sequentially:

```json
{
  "PickingPostProcessing": {
    "Processors": [
      {
        "Id": "packaging-calculator",
        "Assembly": "Customer.Extensions.dll",
        "TypeName": "Customer.Extensions.PackagingCalculatorPostProcessor",
        "Enabled": true
      },
      {
        "Id": "inventory-sync",
        "Assembly": "Customer.Extensions.dll", 
        "TypeName": "Customer.Extensions.InventorySyncPostProcessor",
        "Enabled": true
      }
    ]
  }
}
```

### Conditional Processing

Use the `IsEnabled` method for dynamic enablement:

```csharp
public bool IsEnabled(Dictionary<string, object>? configuration)
{
    if (configuration == null) return false;
    
    // Only enable for specific warehouses
    var warehouse = configuration.GetValueOrDefault("Warehouse")?.ToString();
    return warehouse == "MAIN";
}
```

### External Dependencies

Reference additional packages in your extension project as needed:

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
```

## Support

For issues specific to the post-processing framework, check the application logs for detailed error messages. For custom processor logic, implement comprehensive logging and error handling within your processors.