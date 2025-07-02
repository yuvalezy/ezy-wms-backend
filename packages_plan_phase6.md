# Phase 6: Configuration System & Final Integration

## 6.1 Configuration Management

### 6.1.1 Complete appsettings.json Structure

```json
{
  "Options": {
    "enablePackages": true
  },
  "Package": {
    "Barcode": {
      "Prefix": "PKG",
      "Length": 14,
      "Suffix": "",
      "StartNumber": 1,
      "CheckDigit": false,
      "CheckDigitAlgorithm": "Modulo10",
      "CounterSettings": {
        "UseCounterTable": true,
        "ResetPolicy": "Never", // Never, Daily, Monthly, Yearly
        "CounterName": "Default",
        "BatchSize": 100,
        "MaxRetries": 3,
        "LockTimeoutMs": 5000
      }
    },
    "Label": {
      "AutoPrint": false,
      "Width": 400,
      "Height": 600,
      "BarcodeType": "QR",
      "ShowBarcode": true,
      "BarcodeWidth": 150,
      "BarcodeHeight": 150,
      "Columns": 2,
      "MaxContentLines": 10,
      "FontSize": 10,
      "HeaderFontSize": 12,
      "IncludeCreatedDate": true,
      "IncludeLocation": true,
      "IncludeStatus": true,
      "IncludeContents": true,
      "LogoPath": "",
      "BorderWidth": 2,
      "Margin": 10,
      "PrinterName": "Default",
      "CustomFields": [
        {
          "Name": "CreatedBy",
          "DisplayName": "Created By",
          "Enabled": true,
          "Order": 1
        },
        {
          "Name": "Priority",
          "DisplayName": "Priority",
          "Enabled": true,
          "Order": 2
        },
        {
          "Name": "Notes",
          "DisplayName": "Notes",
          "Enabled": false,
          "Order": 3
        }
      ],
      "Templates": {
        "Standard": {
          "Name": "Standard Label",
          "Width": 400,
          "Height": 600,
          "Description": "Standard package label with barcode and contents"
        },
        "Compact": {
          "Name": "Compact Label",
          "Width": 300,
          "Height": 400,
          "Description": "Compact label for small packages"
        },
        "Detailed": {
          "Name": "Detailed Label",
          "Width": 500,
          "Height": 800,
          "Description": "Detailed label with full contents listing"
        }
      }
    },
    "Validation": {
      "IntervalMinutes": 30,
      "EnableRealTimeValidation": true,
      "EnableScheduledValidation": true,
      "AutoLockInconsistentPackages": true,
      "LockSeverityThreshold": "High",
      "MaxValidationRetries": 3,
      "ValidationTimeout": 30000,
      "NotificationSettings": {
        "EmailEnabled": false,
        "EmailRecipients": [],
        "SlackEnabled": false,
        "SlackWebhookUrl": "",
        "TeamsEnabled": false,
        "TeamsWebhookUrl": ""
      }
    },
    "CustomAttributes": [
      {
        "Name": "Priority",
        "Type": "String",
        "Required": false,
        "DefaultValue": "Normal",
        "AllowedValues": ["Low", "Normal", "High", "Urgent"],
        "Description": "Package priority level",
        "DisplayOrder": 1,
        "ShowOnLabel": true,
        "ShowOnReports": true
      },
      {
        "Name": "Temperature",
        "Type": "String",
        "Required": false,
        "AllowedValues": ["Ambient", "Refrigerated", "Frozen"],
        "Description": "Temperature storage requirement",
        "DisplayOrder": 2,
        "ShowOnLabel": true,
        "ShowOnReports": true
      },
      {
        "Name": "CustomerReference",
        "Type": "String",
        "Required": false,
        "MaxLength": 50,
        "Description": "Customer reference number",
        "DisplayOrder": 3,
        "ShowOnLabel": false,
        "ShowOnReports": true
      },
      {
        "Name": "ExpiryAlert",
        "Type": "Date",
        "Required": false,
        "Description": "Package expiry alert date",
        "DisplayOrder": 4,
        "ShowOnLabel": false,
        "ShowOnReports": true
      },
      {
        "Name": "WeightKg",
        "Type": "Decimal",
        "Required": false,
        "MinValue": 0,
        "MaxValue": 9999.99,
        "DecimalPlaces": 2,
        "Description": "Package weight in kilograms",
        "DisplayOrder": 5,
        "ShowOnLabel": true,
        "ShowOnReports": true
      },
      {
        "Name": "Fragile",
        "Type": "Boolean",
        "Required": false,
        "DefaultValue": "false",
        "Description": "Indicates if package contains fragile items",
        "DisplayOrder": 6,
        "ShowOnLabel": true,
        "ShowOnReports": true
      }
    ],
    "BusinessRules": {
      "MaxItemsPerPackage": 100,
      "MaxPackageWeight": 50.0,
      "AllowNegativeQuantities": false,
      "RequireLocationForPackage": true,
      "AllowCrossWarehouseTransfer": true,
      "RequireApprovalForHighValuePackages": false,
      "HighValueThreshold": 10000.0,
      "AutoCloseEmptyPackages": true,
      "PackageRetentionDays": 365,
      "AllowPackageModificationAfterClosure": false
    },
    "Integration": {
      "SapValidationEnabled": true,
      "SapValidationMode": "RealTime", // RealTime, Scheduled, Manual
      "ExternalSystemNotifications": {
        "Enabled": false,
        "WebhookUrl": "",
        "AuthToken": "",
        "Events": ["PackageCreated", "PackageClosed", "PackageLocked"]
      }
    },
    "Performance": {
      "CacheEnabled": true,
      "CacheDurationMinutes": 15,
      "BatchProcessingSize": 100,
      "MaxConcurrentValidations": 5,
      "QueryTimeoutSeconds": 30
    }
  }
}
```

### 6.1.2 Configuration Models

```csharp
public class PackageConfiguration
{
    public PackageBarcodeSettings Barcode { get; set; } = new();
    public PackageLabelSettings Label { get; set; } = new();
    public PackageValidationSettings Validation { get; set; } = new();
    public List<CustomAttributeDefinition> CustomAttributes { get; set; } = new();
    public PackageBusinessRules BusinessRules { get; set; } = new();
    public PackageIntegrationSettings Integration { get; set; } = new();
    public PackagePerformanceSettings Performance { get; set; } = new();
}

public class PackageBarcodeSettings
{
    public string Prefix { get; set; } = "PKG";
    public int Length { get; set; } = 14;
    public string Suffix { get; set; } = "";
    public long StartNumber { get; set; } = 1;
    public bool CheckDigit { get; set; } = false;
    public string CheckDigitAlgorithm { get; set; } = "Modulo10";
    public PackageBarcodeCounterSettings CounterSettings { get; set; } = new();
}

public class PackageBarcodeCounterSettings
{
    public bool UseCounterTable { get; set; } = true;
    public string ResetPolicy { get; set; } = "Never"; // Never, Daily, Monthly, Yearly
    public string CounterName { get; set; } = "Default";
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 3;
    public int LockTimeoutMs { get; set; } = 5000;
}

public class PackageLabelSettings
{
    public bool AutoPrint { get; set; } = false;
    public int Width { get; set; } = 400;
    public int Height { get; set; } = 600;
    public string BarcodeType { get; set; } = "QR";
    public bool ShowBarcode { get; set; } = true;
    public int BarcodeWidth { get; set; } = 150;
    public int BarcodeHeight { get; set; } = 150;
    public int Columns { get; set; } = 2;
    public int MaxContentLines { get; set; } = 10;
    public int FontSize { get; set; } = 10;
    public int HeaderFontSize { get; set; } = 12;
    public bool IncludeCreatedDate { get; set; } = true;
    public bool IncludeLocation { get; set; } = true;
    public bool IncludeStatus { get; set; } = true;
    public bool IncludeContents { get; set; } = true;
    public string LogoPath { get; set; } = "";
    public int BorderWidth { get; set; } = 2;
    public int Margin { get; set; } = 10;
    public string PrinterName { get; set; } = "Default";
    public List<LabelCustomField> CustomFields { get; set; } = new();
    public Dictionary<string, LabelTemplate> Templates { get; set; } = new();
}

public class PackageValidationSettings
{
    public int IntervalMinutes { get; set; } = 30;
    public bool EnableRealTimeValidation { get; set; } = true;
    public bool EnableScheduledValidation { get; set; } = true;
    public bool AutoLockInconsistentPackages { get; set; } = true;
    public string LockSeverityThreshold { get; set; } = "High";
    public int MaxValidationRetries { get; set; } = 3;
    public int ValidationTimeout { get; set; } = 30000;
    public ValidationNotificationSettings NotificationSettings { get; set; } = new();
}

public class ValidationNotificationSettings
{
    public bool EmailEnabled { get; set; } = false;
    public List<string> EmailRecipients { get; set; } = new();
    public bool SlackEnabled { get; set; } = false;
    public string SlackWebhookUrl { get; set; } = "";
    public bool TeamsEnabled { get; set; } = false;
    public string TeamsWebhookUrl { get; set; } = "";
}

public class CustomAttributeDefinition
{
    public string Name { get; set; }
    public string Type { get; set; } // String, Number, Date, Boolean, Decimal
    public bool Required { get; set; }
    public string DefaultValue { get; set; }
    public List<string> AllowedValues { get; set; } = new();
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? DecimalPlaces { get; set; }
    public string Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool ShowOnLabel { get; set; } = false;
    public bool ShowOnReports { get; set; } = true;
}

public class PackageBusinessRules
{
    public int MaxItemsPerPackage { get; set; } = 100;
    public decimal MaxPackageWeight { get; set; } = 50.0m;
    public bool AllowNegativeQuantities { get; set; } = false;
    public bool RequireLocationForPackage { get; set; } = true;
    public bool AllowCrossWarehouseTransfer { get; set; } = true;
    public bool RequireApprovalForHighValuePackages { get; set; } = false;
    public decimal HighValueThreshold { get; set; } = 10000.0m;
    public bool AutoCloseEmptyPackages { get; set; } = true;
    public int PackageRetentionDays { get; set; } = 365;
    public bool AllowPackageModificationAfterClosure { get; set; } = false;
}

public class PackageIntegrationSettings
{
    public bool SapValidationEnabled { get; set; } = true;
    public string SapValidationMode { get; set; } = "RealTime";
    public ExternalSystemNotificationSettings ExternalSystemNotifications { get; set; } = new();
}

public class ExternalSystemNotificationSettings
{
    public bool Enabled { get; set; } = false;
    public string WebhookUrl { get; set; } = "";
    public string AuthToken { get; set; } = "";
    public List<string> Events { get; set; } = new();
}

public class PackagePerformanceSettings
{
    public bool CacheEnabled { get; set; } = true;
    public int CacheDurationMinutes { get; set; } = 15;
    public int BatchProcessingSize { get; set; } = 100;
    public int MaxConcurrentValidations { get; set; } = 5;
    public int QueryTimeoutSeconds { get; set; } = 30;
}
```

### 6.1.3 Configuration Service

```csharp
public interface IPackageConfigurationService
{
    PackageConfiguration GetConfiguration();
    Task<bool> UpdateConfigurationAsync(PackageConfiguration configuration);
    Task<bool> ValidateConfigurationAsync(PackageConfiguration configuration);
    Task<ConfigurationValidationResult> ValidateCustomAttributesAsync(List<CustomAttributeDefinition> attributes);
    Task ReloadConfigurationAsync();
}

public class PackageConfigurationService : IPackageConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PackageConfigurationService> _logger;
    private readonly IOptionsMonitor<PackageConfiguration> _packageConfig;
    private readonly IMemoryCache _cache;
    
    public PackageConfigurationService(
        IConfiguration configuration,
        ILogger<PackageConfigurationService> logger,
        IOptionsMonitor<PackageConfiguration> packageConfig,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _packageConfig = packageConfig;
        _cache = cache;
    }
    
    public PackageConfiguration GetConfiguration()
    {
        return _cache.GetOrCreate("PackageConfiguration", factory =>
        {
            factory.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return _packageConfig.CurrentValue;
        });
    }
    
    public async Task<bool> UpdateConfigurationAsync(PackageConfiguration configuration)
    {
        try
        {
            var validationResult = await ValidateConfigurationAsync(configuration);
            if (!validationResult)
            {
                return false;
            }
            
            // In a real implementation, this would update the configuration source
            // For now, we'll just update the cache
            _cache.Set("PackageConfiguration", configuration, TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Package configuration updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating package configuration");
            return false;
        }
    }
    
    public async Task<bool> ValidateConfigurationAsync(PackageConfiguration configuration)
    {
        try
        {
            // Validate barcode settings
            if (configuration.Barcode.Length < configuration.Barcode.Prefix.Length + configuration.Barcode.Suffix.Length + 1)
            {
                _logger.LogError("Barcode length is too short for prefix and suffix");
                return false;
            }
            
            // Validate label settings
            if (configuration.Label.Width <= 0 || configuration.Label.Height <= 0)
            {
                _logger.LogError("Label dimensions must be positive");
                return false;
            }
            
            // Validate custom attributes
            var attributeValidation = await ValidateCustomAttributesAsync(configuration.CustomAttributes);
            if (!attributeValidation.IsValid)
            {
                _logger.LogError("Custom attributes validation failed: {Errors}", 
                    string.Join(", ", attributeValidation.Errors));
                return false;
            }
            
            // Validate business rules
            if (configuration.BusinessRules.MaxItemsPerPackage <= 0)
            {
                _logger.LogError("MaxItemsPerPackage must be positive");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating package configuration");
            return false;
        }
    }
    
    public async Task<ConfigurationValidationResult> ValidateCustomAttributesAsync(List<CustomAttributeDefinition> attributes)
    {
        var result = new ConfigurationValidationResult { IsValid = true };
        
        // Check for duplicate names
        var duplicateNames = attributes
            .GroupBy(a => a.Name.ToLower())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        
        foreach (var duplicateName in duplicateNames)
        {
            result.Errors.Add($"Duplicate custom attribute name: {duplicateName}");
            result.IsValid = false;
        }
        
        // Validate each attribute
        foreach (var attribute in attributes)
        {
            if (string.IsNullOrWhiteSpace(attribute.Name))
            {
                result.Errors.Add("Custom attribute name cannot be empty");
                result.IsValid = false;
                continue;
            }
            
            if (!IsValidAttributeType(attribute.Type))
            {
                result.Errors.Add($"Invalid attribute type: {attribute.Type}");
                result.IsValid = false;
            }
            
            if (attribute.Type == "String" && attribute.MaxLength.HasValue && attribute.MaxLength.Value <= 0)
            {
                result.Errors.Add($"MaxLength must be positive for attribute: {attribute.Name}");
                result.IsValid = false;
            }
            
            if (attribute.Type == "Decimal" && attribute.DecimalPlaces.HasValue && attribute.DecimalPlaces.Value < 0)
            {
                result.Errors.Add($"DecimalPlaces cannot be negative for attribute: {attribute.Name}");
                result.IsValid = false;
            }
            
            if (attribute.MinValue.HasValue && attribute.MaxValue.HasValue && 
                attribute.MinValue.Value > attribute.MaxValue.Value)
            {
                result.Errors.Add($"MinValue cannot be greater than MaxValue for attribute: {attribute.Name}");
                result.IsValid = false;
            }
        }
        
        return result;
    }
    
    public async Task ReloadConfigurationAsync()
    {
        _cache.Remove("PackageConfiguration");
        _logger.LogInformation("Package configuration cache cleared");
    }
    
    private bool IsValidAttributeType(string type)
    {
        var validTypes = new[] { "String", "Number", "Date", "Boolean", "Decimal" };
        return validTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}

public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

## 6.2 Custom Attributes System

### 6.2.1 Custom Attribute Validation Service

```csharp
public interface ICustomAttributeValidationService
{
    Task<AttributeValidationResult> ValidateAttributeValueAsync(string attributeName, object value);
    Task<Dictionary<string, object>> ValidateAndNormalizeAttributesAsync(Dictionary<string, object> attributes);
    Dictionary<string, object> GetDefaultAttributeValues();
    List<CustomAttributeDefinition> GetAttributeDefinitions();
}

public class CustomAttributeValidationService : ICustomAttributeValidationService
{
    private readonly IPackageConfigurationService _configService;
    private readonly ILogger<CustomAttributeValidationService> _logger;
    
    public CustomAttributeValidationService(
        IPackageConfigurationService configService,
        ILogger<CustomAttributeValidationService> logger)
    {
        _configService = configService;
        _logger = logger;
    }
    
    public async Task<AttributeValidationResult> ValidateAttributeValueAsync(string attributeName, object value)
    {
        var config = _configService.GetConfiguration();
        var attributeDefinition = config.CustomAttributes
            .FirstOrDefault(a => a.Name.Equals(attributeName, StringComparison.OrdinalIgnoreCase));
        
        if (attributeDefinition == null)
        {
            return AttributeValidationResult.Success();
        }
        
        try
        {
            // Check required
            if (attributeDefinition.Required && (value == null || string.IsNullOrWhiteSpace(value.ToString())))
            {
                return AttributeValidationResult.Failure($"Attribute '{attributeName}' is required");
            }
            
            // If value is null or empty and not required, it's valid
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return AttributeValidationResult.Success();
            }
            
            // Type-specific validation
            switch (attributeDefinition.Type.ToLower())
            {
                case "string":
                    return ValidateStringAttribute(attributeDefinition, value.ToString());
                
                case "number":
                case "decimal":
                    return ValidateNumericAttribute(attributeDefinition, value);
                
                case "date":
                    return ValidateDateAttribute(attributeDefinition, value);
                
                case "boolean":
                    return ValidateBooleanAttribute(attributeDefinition, value);
                
                default:
                    return AttributeValidationResult.Failure($"Unknown attribute type: {attributeDefinition.Type}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating attribute {AttributeName}", attributeName);
            return AttributeValidationResult.Failure($"Validation error for attribute '{attributeName}': {ex.Message}");
        }
    }
    
    public async Task<Dictionary<string, object>> ValidateAndNormalizeAttributesAsync(Dictionary<string, object> attributes)
    {
        var normalized = new Dictionary<string, object>();
        var config = _configService.GetConfiguration();
        
        // Add default values for all defined attributes
        foreach (var definition in config.CustomAttributes)
        {
            if (!string.IsNullOrEmpty(definition.DefaultValue))
            {
                normalized[definition.Name] = ParseAttributeValue(definition.Type, definition.DefaultValue);
            }
        }
        
        // Validate and add provided attributes
        foreach (var kvp in attributes ?? new Dictionary<string, object>())
        {
            var validationResult = await ValidateAttributeValueAsync(kvp.Key, kvp.Value);
            if (validationResult.IsValid)
            {
                var definition = config.CustomAttributes
                    .FirstOrDefault(a => a.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                
                if (definition != null)
                {
                    normalized[definition.Name] = NormalizeAttributeValue(definition.Type, kvp.Value);
                }
                else
                {
                    // Unknown attribute - include as-is for backward compatibility
                    normalized[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                _logger.LogWarning("Invalid attribute value for {AttributeName}: {Error}", 
                    kvp.Key, validationResult.ErrorMessage);
            }
        }
        
        return normalized;
    }
    
    public Dictionary<string, object> GetDefaultAttributeValues()
    {
        var config = _configService.GetConfiguration();
        var defaults = new Dictionary<string, object>();
        
        foreach (var definition in config.CustomAttributes)
        {
            if (!string.IsNullOrEmpty(definition.DefaultValue))
            {
                defaults[definition.Name] = ParseAttributeValue(definition.Type, definition.DefaultValue);
            }
        }
        
        return defaults;
    }
    
    public List<CustomAttributeDefinition> GetAttributeDefinitions()
    {
        var config = _configService.GetConfiguration();
        return config.CustomAttributes.OrderBy(a => a.DisplayOrder).ToList();
    }
    
    private AttributeValidationResult ValidateStringAttribute(CustomAttributeDefinition definition, string value)
    {
        // Check allowed values
        if (definition.AllowedValues.Any() && !definition.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return AttributeValidationResult.Failure(
                $"Value '{value}' is not allowed. Allowed values: {string.Join(", ", definition.AllowedValues)}");
        }
        
        // Check max length
        if (definition.MaxLength.HasValue && value.Length > definition.MaxLength.Value)
        {
            return AttributeValidationResult.Failure(
                $"Value length ({value.Length}) exceeds maximum length ({definition.MaxLength.Value})");
        }
        
        return AttributeValidationResult.Success();
    }
    
    private AttributeValidationResult ValidateNumericAttribute(CustomAttributeDefinition definition, object value)
    {
        if (!decimal.TryParse(value.ToString(), out var numericValue))
        {
            return AttributeValidationResult.Failure($"Value '{value}' is not a valid number");
        }
        
        // Check range
        if (definition.MinValue.HasValue && numericValue < definition.MinValue.Value)
        {
            return AttributeValidationResult.Failure(
                $"Value ({numericValue}) is less than minimum ({definition.MinValue.Value})");
        }
        
        if (definition.MaxValue.HasValue && numericValue > definition.MaxValue.Value)
        {
            return AttributeValidationResult.Failure(
                $"Value ({numericValue}) is greater than maximum ({definition.MaxValue.Value})");
        }
        
        // Check decimal places
        if (definition.DecimalPlaces.HasValue)
        {
            var decimalPlaces = GetDecimalPlaces(numericValue);
            if (decimalPlaces > definition.DecimalPlaces.Value)
            {
                return AttributeValidationResult.Failure(
                    $"Value has too many decimal places. Maximum: {definition.DecimalPlaces.Value}");
            }
        }
        
        return AttributeValidationResult.Success();
    }
    
    private AttributeValidationResult ValidateDateAttribute(CustomAttributeDefinition definition, object value)
    {
        if (!DateTime.TryParse(value.ToString(), out var dateValue))
        {
            return AttributeValidationResult.Failure($"Value '{value}' is not a valid date");
        }
        
        return AttributeValidationResult.Success();
    }
    
    private AttributeValidationResult ValidateBooleanAttribute(CustomAttributeDefinition definition, object value)
    {
        if (!bool.TryParse(value.ToString(), out var boolValue))
        {
            return AttributeValidationResult.Failure($"Value '{value}' is not a valid boolean");
        }
        
        return AttributeValidationResult.Success();
    }
    
    private object ParseAttributeValue(string type, string value)
    {
        return type.ToLower() switch
        {
            "string" => value,
            "number" => int.TryParse(value, out var intVal) ? intVal : 0,
            "decimal" => decimal.TryParse(value, out var decVal) ? decVal : 0m,
            "date" => DateTime.TryParse(value, out var dateVal) ? dateVal : DateTime.MinValue,
            "boolean" => bool.TryParse(value, out var boolVal) && boolVal,
            _ => value
        };
    }
    
    private object NormalizeAttributeValue(string type, object value)
    {
        return type.ToLower() switch
        {
            "string" => value?.ToString() ?? "",
            "number" => Convert.ToInt32(value),
            "decimal" => Convert.ToDecimal(value),
            "date" => Convert.ToDateTime(value),
            "boolean" => Convert.ToBoolean(value),
            _ => value
        };
    }
    
    private int GetDecimalPlaces(decimal value)
    {
        return BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
    }
}

public class AttributeValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
    
    public static AttributeValidationResult Success() => new() { IsValid = true };
    public static AttributeValidationResult Failure(string message) => new() { IsValid = false, ErrorMessage = message };
}
```

### 6.2.2 Business Rules Engine

```csharp
public interface IPackageBusinessRulesEngine
{
    Task<BusinessRuleValidationResult> ValidatePackageCreationAsync(CreatePackageRequest request);
    Task<BusinessRuleValidationResult> ValidateAddItemToPackageAsync(AddItemToPackageRequest request);
    Task<BusinessRuleValidationResult> ValidatePackageTransferAsync(MovePackageRequest request);
    Task<BusinessRuleValidationResult> ValidatePackageClosureAsync(Guid packageId);
}

public class PackageBusinessRulesEngine : IPackageBusinessRulesEngine
{
    private readonly IPackageConfigurationService _configService;
    private readonly IPackageService _packageService;
    private readonly ILWDbContext _context;
    private readonly ILogger<PackageBusinessRulesEngine> _logger;
    
    public PackageBusinessRulesEngine(
        IPackageConfigurationService configService,
        IPackageService packageService,
        ILWDbContext context,
        ILogger<PackageBusinessRulesEngine> logger)
    {
        _configService = configService;
        _packageService = packageService;
        _context = context;
        _logger = logger;
    }
    
    public async Task<BusinessRuleValidationResult> ValidatePackageCreationAsync(CreatePackageRequest request)
    {
        var config = _configService.GetConfiguration();
        var result = new BusinessRuleValidationResult { IsValid = true };
        
        // Validate location requirement
        if (config.BusinessRules.RequireLocationForPackage)
        {
            if (string.IsNullOrEmpty(request.WhsCode))
            {
                result.AddError("Warehouse code is required for package creation");
            }
        }
        
        // Validate custom attributes if provided
        if (request.CustomAttributes?.Any() == true)
        {
            foreach (var attr in request.CustomAttributes)
            {
                // Additional business rule validation for custom attributes
                await ValidateCustomAttributeBusinessRules(attr.Key, attr.Value, result);
            }
        }
        
        return result;
    }
    
    public async Task<BusinessRuleValidationResult> ValidateAddItemToPackageAsync(AddItemToPackageRequest request)
    {
        var config = _configService.GetConfiguration();
        var result = new BusinessRuleValidationResult { IsValid = true };
        
        var package = await _packageService.GetPackageAsync(request.PackageId);
        if (package == null)
        {
            result.AddError("Package not found");
            return result;
        }
        
        // Check maximum items per package
        var currentItemCount = await _context.PackageContents
            .CountAsync(c => c.PackageId == request.PackageId);
        
        if (currentItemCount >= config.BusinessRules.MaxItemsPerPackage)
        {
            result.AddError($"Package already contains maximum number of items ({config.BusinessRules.MaxItemsPerPackage})");
        }
        
        // Check negative quantities
        if (!config.BusinessRules.AllowNegativeQuantities && request.Quantity < 0)
        {
            result.AddError("Negative quantities are not allowed");
        }
        
        // Check package weight if configured
        if (config.BusinessRules.MaxPackageWeight > 0)
        {
            var currentWeight = await CalculatePackageWeightAsync(request.PackageId);
            var itemWeight = await GetItemWeightAsync(request.ItemCode, request.Quantity);
            
            if (currentWeight + itemWeight > config.BusinessRules.MaxPackageWeight)
            {
                result.AddError($"Adding this item would exceed maximum package weight ({config.BusinessRules.MaxPackageWeight} kg)");
            }
        }
        
        return result;
    }
    
    public async Task<BusinessRuleValidationResult> ValidatePackageTransferAsync(MovePackageRequest request)
    {
        var config = _configService.GetConfiguration();
        var result = new BusinessRuleValidationResult { IsValid = true };
        
        var package = await _packageService.GetPackageAsync(request.PackageId);
        if (package == null)
        {
            result.AddError("Package not found");
            return result;
        }
        
        // Check cross-warehouse transfer rules
        if (!config.BusinessRules.AllowCrossWarehouseTransfer && 
            package.WhsCode != request.ToWhsCode)
        {
            result.AddError("Cross-warehouse transfers are not allowed");
        }
        
        // Check if package is locked or closed
        if (package.Status == PackageStatus.Locked)
        {
            result.AddError("Cannot transfer locked package");
        }
        
        if (package.Status == PackageStatus.Closed)
        {
            result.AddError("Cannot transfer closed package");
        }
        
        return result;
    }
    
    public async Task<BusinessRuleValidationResult> ValidatePackageClosureAsync(Guid packageId)
    {
        var config = _configService.GetConfiguration();
        var result = new BusinessRuleValidationResult { IsValid = true };
        
        var package = await _packageService.GetPackageAsync(packageId);
        if (package == null)
        {
            result.AddError("Package not found");
            return result;
        }
        
        // Check if package can be modified after closure
        if (package.Status == PackageStatus.Closed && 
            !config.BusinessRules.AllowPackageModificationAfterClosure)
        {
            result.AddError("Package is already closed and modifications are not allowed");
        }
        
        // Check high-value package approval requirement
        if (config.BusinessRules.RequireApprovalForHighValuePackages)
        {
            var packageValue = await CalculatePackageValueAsync(packageId);
            if (packageValue > config.BusinessRules.HighValueThreshold)
            {
                // Check if approval exists (would need approval tracking system)
                var hasApproval = await CheckPackageApprovalAsync(packageId);
                if (!hasApproval)
                {
                    result.AddError($"High-value package (${packageValue:N2}) requires approval before closure");
                }
            }
        }
        
        return result;
    }
    
    private async Task ValidateCustomAttributeBusinessRules(string attributeName, object value, BusinessRuleValidationResult result)
    {
        // Implement specific business rules for custom attributes
        switch (attributeName.ToLower())
        {
            case "priority":
                if (value?.ToString() == "Urgent")
                {
                    // Could check if user has permission to create urgent packages
                    // result.AddError("Insufficient privileges to create urgent packages");
                }
                break;
                
            case "temperature":
                if (value?.ToString() == "Frozen")
                {
                    // Could check if location supports frozen storage
                    // result.AddError("Selected location does not support frozen storage");
                }
                break;
        }
    }
    
    private async Task<decimal> CalculatePackageWeightAsync(Guid packageId)
    {
        // Implementation would calculate total weight based on item weights
        // For now, return 0 as this requires item master data integration
        return 0;
    }
    
    private async Task<decimal> GetItemWeightAsync(string itemCode, decimal quantity)
    {
        // Implementation would look up item weight from item master
        // For now, return 0
        return 0;
    }
    
    private async Task<decimal> CalculatePackageValueAsync(Guid packageId)
    {
        // Implementation would calculate total value based on item costs
        // For now, return 0 as this requires item master data integration
        return 0;
    }
    
    private async Task<bool> CheckPackageApprovalAsync(Guid packageId)
    {
        // Implementation would check approval tracking system
        // For now, return true (no approval required)
        return true;
    }
}

public class BusinessRuleValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }
    
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}
```

## 6.3 External System Integration

### 6.3.1 Webhook Notification Service

```csharp
public interface IExternalNotificationService
{
    Task NotifyPackageEventAsync(PackageEventNotification notification);
    Task<bool> TestWebhookConnectionAsync();
}

public class ExternalNotificationService : IExternalNotificationService
{
    private readonly IPackageConfigurationService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalNotificationService> _logger;
    
    public ExternalNotificationService(
        IPackageConfigurationService configService,
        IHttpClientFactory httpClientFactory,
        ILogger<ExternalNotificationService> logger)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    
    public async Task NotifyPackageEventAsync(PackageEventNotification notification)
    {
        var config = _configService.GetConfiguration();
        
        if (!config.Integration.ExternalSystemNotifications.Enabled)
        {
            return;
        }
        
        if (!config.Integration.ExternalSystemNotifications.Events.Contains(notification.EventType))
        {
            return;
        }
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            if (!string.IsNullOrEmpty(config.Integration.ExternalSystemNotifications.AuthToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", 
                        config.Integration.ExternalSystemNotifications.AuthToken);
            }
            
            var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync(
                config.Integration.ExternalSystemNotifications.WebhookUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("External notification sent successfully for event {EventType}, Package {PackageBarcode}", 
                    notification.EventType, notification.PackageBarcode);
            }
            else
            {
                _logger.LogWarning("External notification failed for event {EventType}, Package {PackageBarcode}. Status: {StatusCode}", 
                    notification.EventType, notification.PackageBarcode, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending external notification for event {EventType}, Package {PackageBarcode}", 
                notification.EventType, notification.PackageBarcode);
        }
    }
    
    public async Task<bool> TestWebhookConnectionAsync()
    {
        var config = _configService.GetConfiguration();
        
        if (!config.Integration.ExternalSystemNotifications.Enabled || 
            string.IsNullOrEmpty(config.Integration.ExternalSystemNotifications.WebhookUrl))
        {
            return false;
        }
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            var testNotification = new PackageEventNotification
            {
                EventType = "Test",
                PackageId = Guid.NewGuid(),
                PackageBarcode = "TEST_PACKAGE",
                Timestamp = DateTime.UtcNow,
                Data = new Dictionary<string, object> { ["test"] = true }
            };
            
            var json = JsonSerializer.Serialize(testNotification);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync(
                config.Integration.ExternalSystemNotifications.WebhookUrl, content);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing webhook connection");
            return false;
        }
    }
}

public class PackageEventNotification
{
    public string EventType { get; set; }
    public Guid PackageId { get; set; }
    public string PackageBarcode { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}
```

## 6.3.5 Barcode Counter System

### Database Schema

```sql
CREATE TABLE PackageBarcodeCounter (
    CounterName NVARCHAR(50) NOT NULL PRIMARY KEY,
    CurrentValue BIGINT NOT NULL DEFAULT 0,
    BatchSize INT NOT NULL DEFAULT 100,
    ResetPolicy NVARCHAR(20) NOT NULL DEFAULT 'Never', -- Never, Daily, Monthly, Yearly
    LastResetDate DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LockedBy NVARCHAR(255) NULL,
    LockedAt DATETIME2 NULL,
    LockExpiresAt DATETIME2 NULL,
    
    INDEX IX_PackageBarcodeCounter_CounterName (CounterName),
    INDEX IX_PackageBarcodeCounter_LockedBy (LockedBy),
    INDEX IX_PackageBarcodeCounter_LockExpiresAt (LockExpiresAt)
);

-- Initialize default counter
INSERT INTO PackageBarcodeCounter (CounterName, CurrentValue) 
VALUES ('Default', 0);
```

### Entity Model

```csharp
public class PackageBarcodeCounter
{
    public string CounterName { get; set; }
    public long CurrentValue { get; set; }
    public int BatchSize { get; set; } = 100;
    public string ResetPolicy { get; set; } = "Never";
    public DateTime? LastResetDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? LockExpiresAt { get; set; }
}
```

### Counter Service Implementation

```csharp
public interface IPackageBarcodeCounterService
{
    Task<long> GetNextNumberAsync(string counterName = "Default");
    Task<IEnumerable<long>> GetNextNumberBatchAsync(int count, string counterName = "Default");
    Task ResetCounterAsync(string counterName, long newValue = 0);
    Task<PackageBarcodeCounter> GetCounterStatusAsync(string counterName);
    Task InitializeCounterAsync(string counterName, long startValue = 0);
}

public class PackageBarcodeCounterService : IPackageBarcodeCounterService
{
    private readonly IDbContext _context;
    private readonly IPackageConfigurationService _configService;
    private readonly ILogger<PackageBarcodeCounterService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    public PackageBarcodeCounterService(
        IDbContext context,
        IPackageConfigurationService configService,
        ILogger<PackageBarcodeCounterService> logger)
    {
        _context = context;
        _configService = configService;
        _logger = logger;
    }
    
    public async Task<long> GetNextNumberAsync(string counterName = "Default")
    {
        var batch = await GetNextNumberBatchAsync(1, counterName);
        return batch.First();
    }
    
    public async Task<IEnumerable<long>> GetNextNumberBatchAsync(int count, string counterName = "Default")
    {
        var config = _configService.GetConfiguration();
        var settings = config.Barcode.CounterSettings;
        
        if (!settings.UseCounterTable)
        {
            // Fallback to old method
            return await GetNextNumbersLegacyAsync(count);
        }
        
        await _semaphore.WaitAsync();
        try
        {
            return await GetNextNumbersWithLockingAsync(count, counterName, settings);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private async Task<IEnumerable<long>> GetNextNumbersWithLockingAsync(
        int count, 
        string counterName, 
        PackageBarcodeCounterSettings settings)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var lockId = Guid.NewGuid().ToString();
            var lockExpiry = DateTime.UtcNow.AddMilliseconds(settings.LockTimeoutMs);
            
            // Try to acquire lock with timeout
            var lockAcquired = await TryAcquireLockAsync(counterName, lockId, lockExpiry, settings);
            if (!lockAcquired)
            {
                throw new InvalidOperationException($"Could not acquire lock for counter '{counterName}' within timeout");
            }
            
            try
            {
                // Get or create counter
                var counter = await _context.PackageBarcodeCounters
                    .FirstOrDefaultAsync(c => c.CounterName == counterName);
                
                if (counter == null)
                {
                    counter = new PackageBarcodeCounter
                    {
                        CounterName = counterName,
                        CurrentValue = 0,
                        BatchSize = settings.BatchSize,
                        ResetPolicy = settings.ResetPolicy
                    };
                    _context.PackageBarcodeCounters.Add(counter);
                }
                
                // Check if reset is needed
                await CheckAndApplyResetPolicyAsync(counter);
                
                // Generate range of numbers
                var startValue = counter.CurrentValue + 1;
                var endValue = counter.CurrentValue + count;
                var numbers = Enumerable.Range((int)startValue, count).Select(i => (long)i);
                
                // Update counter
                counter.CurrentValue = endValue;
                counter.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogDebug("Generated {Count} barcode numbers from {Start} to {End} for counter {CounterName}", 
                    count, startValue, endValue, counterName);
                
                return numbers;
            }
            finally
            {
                // Release lock
                await ReleaseLockAsync(counterName, lockId);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error generating barcode numbers for counter {CounterName}", counterName);
            throw;
        }
    }
    
    private async Task<bool> TryAcquireLockAsync(
        string counterName, 
        string lockId, 
        DateTime lockExpiry, 
        PackageBarcodeCounterSettings settings)
    {
        for (int attempt = 0; attempt < settings.MaxRetries; attempt++)
        {
            try
            {
                // Clean up expired locks first
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE PackageBarcodeCounter SET LockedBy = NULL, LockedAt = NULL, LockExpiresAt = NULL " +
                    "WHERE CounterName = {0} AND LockExpiresAt < {1}", 
                    counterName, DateTime.UtcNow);
                
                // Try to acquire lock
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE PackageBarcodeCounter SET LockedBy = {0}, LockedAt = {1}, LockExpiresAt = {2} " +
                    "WHERE CounterName = {3} AND LockedBy IS NULL",
                    lockId, DateTime.UtcNow, lockExpiry, counterName);
                
                if (rowsAffected > 0)
                {
                    return true;
                }
                
                // Wait before retry
                if (attempt < settings.MaxRetries - 1)
                {
                    await Task.Delay(100 * (attempt + 1)); // Exponential backoff
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} to acquire lock failed for counter {CounterName}", 
                    attempt + 1, counterName);
            }
        }
        
        return false;
    }
    
    private async Task ReleaseLockAsync(string counterName, string lockId)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE PackageBarcodeCounter SET LockedBy = NULL, LockedAt = NULL, LockExpiresAt = NULL " +
                "WHERE CounterName = {0} AND LockedBy = {1}",
                counterName, lockId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release lock for counter {CounterName}", counterName);
        }
    }
    
    private async Task CheckAndApplyResetPolicyAsync(PackageBarcodeCounter counter)
    {
        if (counter.ResetPolicy == "Never")
            return;
        
        var now = DateTime.UtcNow;
        var shouldReset = counter.ResetPolicy switch
        {
            "Daily" => counter.LastResetDate?.Date != now.Date,
            "Monthly" => counter.LastResetDate?.Month != now.Month || counter.LastResetDate?.Year != now.Year,
            "Yearly" => counter.LastResetDate?.Year != now.Year,
            _ => false
        };
        
        if (shouldReset)
        {
            _logger.LogInformation("Resetting counter {CounterName} due to {ResetPolicy} policy", 
                counter.CounterName, counter.ResetPolicy);
            
            counter.CurrentValue = 0;
            counter.LastResetDate = now;
        }
    }
    
    private async Task<IEnumerable<long>> GetNextNumbersLegacyAsync(int count)
    {
        // Fallback to querying last package - less efficient but maintains compatibility
        var lastPackage = await _context.Packages
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
        
        var lastNumber = ExtractNumberFromBarcode(lastPackage?.Barcode) ?? 0;
        return Enumerable.Range((int)(lastNumber + 1), count).Select(i => (long)i);
    }
    
    private long? ExtractNumberFromBarcode(string barcode)
    {
        if (string.IsNullOrEmpty(barcode))
            return null;
        
        var config = _configService.GetConfiguration();
        var prefix = config.Barcode.Prefix;
        var suffix = config.Barcode.Suffix;
        
        if (barcode.StartsWith(prefix) && barcode.EndsWith(suffix))
        {
            var numberPart = barcode.Substring(prefix.Length, 
                barcode.Length - prefix.Length - suffix.Length);
            
            if (long.TryParse(numberPart, out var number))
                return number;
        }
        
        return null;
    }
    
    public async Task ResetCounterAsync(string counterName, long newValue = 0)
    {
        var counter = await _context.PackageBarcodeCounters
            .FirstOrDefaultAsync(c => c.CounterName == counterName);
        
        if (counter != null)
        {
            counter.CurrentValue = newValue;
            counter.LastResetDate = DateTime.UtcNow;
            counter.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Counter {CounterName} reset to {NewValue}", counterName, newValue);
        }
    }
    
    public async Task<PackageBarcodeCounter> GetCounterStatusAsync(string counterName)
    {
        return await _context.PackageBarcodeCounters
            .FirstOrDefaultAsync(c => c.CounterName == counterName);
    }
    
    public async Task InitializeCounterAsync(string counterName, long startValue = 0)
    {
        var existingCounter = await _context.PackageBarcodeCounters
            .FirstOrDefaultAsync(c => c.CounterName == counterName);
        
        if (existingCounter == null)
        {
            var counter = new PackageBarcodeCounter
            {
                CounterName = counterName,
                CurrentValue = startValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.PackageBarcodeCounters.Add(counter);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Initialized counter {CounterName} with start value {StartValue}", 
                counterName, startValue);
        }
    }
}
```

### Updated GeneratePackageBarcodeAsync Implementation

```csharp
public class PackageService : IPackageService
{
    private readonly IPackageBarcodeCounterService _counterService;
    
    public async Task<string> GeneratePackageBarcodeAsync()
    {
        var barcodeSettings = settings.Package.Barcode;
        long nextNumber;
        
        if (barcodeSettings.CounterSettings.UseCounterTable)
        {
            // Use efficient counter-based approach
            nextNumber = await _counterService.GetNextNumberAsync(barcodeSettings.CounterSettings.CounterName);
        }
        else
        {
            // Fallback to legacy approach for backward compatibility
            long lastNumber = await GetLastPackageNumberAsync();
            nextNumber = lastNumber + 1;
        }

        string numberPart = nextNumber.ToString().PadLeft(
            barcodeSettings.Length - barcodeSettings.Prefix.Length - barcodeSettings.Suffix.Length, '0');

        var barcode = $"{barcodeSettings.Prefix}{numberPart}{barcodeSettings.Suffix}";
        
        // Add check digit if enabled
        if (barcodeSettings.CheckDigit)
        {
            var checkDigit = CalculateCheckDigit(barcode, barcodeSettings.CheckDigitAlgorithm);
            barcode += checkDigit;
        }
        
        return barcode;
    }
    
    private async Task<long> GetLastPackageNumberAsync()
    {
        // Legacy method - kept for backward compatibility
        var lastPackage = await context.Packages
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
            
        return ExtractNumberFromBarcode(lastPackage?.Barcode) ?? 0;
    }
}
```

## 6.4 Final Integration & Startup Configuration

### 6.4.1 Service Registration

```csharp
// In Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register package configuration
    services.Configure<PackageConfiguration>(Configuration.GetSection("Package"));
    services.Configure<PackageBarcodeSettings>(Configuration.GetSection("Package:Barcode"));
    services.Configure<PackageLabelSettings>(Configuration.GetSection("Package:Label"));
    services.Configure<PackageValidationSettings>(Configuration.GetSection("Package:Validation"));
    
    // Register core package services
    services.AddScoped<IPackageService, PackageService>();
    services.AddScoped<IPackageConfigurationService, PackageConfigurationService>();
    services.AddScoped<ICustomAttributeValidationService, CustomAttributeValidationService>();
    services.AddScoped<IPackageBusinessRulesEngine, PackageBusinessRulesEngine>();
    
    // Register validation services
    services.AddScoped<IPackageConsistencyService, PackageConsistencyService>();
    services.AddScoped<PackageOperationValidator>();
    
    // Register report and label services
    services.AddScoped<IPackageReportService, PackageReportService>();
    services.AddScoped<IPackageLabelService, PackageLabelService>();
    services.AddScoped<IPrintService, PrintService>();
    
    // Register external integration services
    services.AddScoped<IExternalNotificationService, ExternalNotificationService>();
    services.AddHttpClient();
    
    // Register background services
    if (Configuration.GetValue<bool>("Package:Validation:EnableScheduledValidation", true))
    {
        services.AddHostedService<PackageValidationHostedService>();
    }
    
    services.AddHostedService<RealTimePackageValidationService>();
    
    // Register memory cache for configuration caching
    services.AddMemoryCache();
    
    // Register Entity Framework context with package entities
    services.AddDbContext<ILWDbContext>(options =>
    {
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
        // Configure package entities
        options.EnableSensitiveDataLogging(isDevelopment);
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ... existing configuration ...
    
    // Ensure package feature is properly initialized
    using (var scope = app.ApplicationServices.CreateScope())
    {
        var configService = scope.ServiceProvider.GetRequiredService<IPackageConfigurationService>();
        var config = configService.GetConfiguration();
        
        if (Configuration.GetValue<bool>("Options:enablePackages", false))
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Startup>>();
            logger.LogInformation("Package management system initialized successfully");
            logger.LogInformation("Package barcode format: {Prefix}[number]{Suffix}, Length: {Length}", 
                config.Barcode.Prefix, config.Barcode.Suffix, config.Barcode.Length);
            logger.LogInformation("Package validation: Real-time: {RealTime}, Scheduled: {Scheduled}", 
                config.Validation.EnableRealTimeValidation, config.Validation.EnableScheduledValidation);
        }
    }
}
```

### 6.4.2 Migration and Database Initialization

```csharp
public class PackageSystemInitializer
{
    private readonly ILWDbContext _context;
    private readonly IPackageConfigurationService _configService;
    private readonly ILogger<PackageSystemInitializer> _logger;
    
    public PackageSystemInitializer(
        ILWDbContext context,
        IPackageConfigurationService configService,
        ILogger<PackageSystemInitializer> logger)
    {
        _context = context;
        _configService = configService;
        _logger = logger;
    }
    
    public async Task InitializeAsync()
    {
        try
        {
            // Ensure package tables exist
            await EnsurePackageTablesAsync();
            
            // Validate configuration
            var config = _configService.GetConfiguration();
            var isValid = await _configService.ValidateConfigurationAsync(config);
            
            if (!isValid)
            {
                _logger.LogWarning("Package configuration validation failed. Some features may not work correctly.");
            }
            
            // Initialize default data if needed
            await InitializeDefaultDataAsync();
            
            _logger.LogInformation("Package system initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during package system initialization");
            throw;
        }
    }
    
    private async Task EnsurePackageTablesAsync()
    {
        // Check if package tables exist
        var packageTableExists = await _context.Database
            .ExecuteSqlRawAsync("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Package'") > 0;
        
        if (!packageTableExists)
        {
            _logger.LogInformation("Package tables not found. Running migrations...");
            await _context.Database.MigrateAsync();
        }
    }
    
    private async Task InitializeDefaultDataAsync()
    {
        // Initialize any default data that might be needed
        // For example, default package templates, sample custom attributes, etc.
        
        _logger.LogInformation("Default package data initialization completed");
    }
}
```

## Implementation Notes

### Timeline: Week 12
- Complete configuration system with validation
- Custom attributes system with business rule validation
- External system integration via webhooks
- Final testing and deployment preparation
- Documentation and configuration examples

### Key Features
- **Comprehensive Configuration**: Full appsettings.json structure with validation
- **Custom Attributes**: Flexible, validated custom fields with business rules
- **Business Rules Engine**: Configurable validation for complex business scenarios
- **External Integration**: Webhook notifications for package events
- **Performance Optimization**: Caching, batch processing, and timeout controls
- **Startup Validation**: Ensure system is properly configured on startup

### Configuration Capabilities
- Barcode format customization with validation
- Label template system with multiple designs
- Validation settings (real-time, scheduled, thresholds)
- Custom attributes with type validation and business rules
- Business rules (weight limits, item counts, approval workflows)
- External system integration settings
- Performance tuning parameters

### Production Readiness
- Configuration validation on startup
- Error handling and logging throughout
- Background service management
- Database migration support
- External system connectivity testing
- Comprehensive monitoring and alerting hooks

This completes the comprehensive package management system implementation plan with full configuration management, custom attributes, business rules, and external integration capabilities.