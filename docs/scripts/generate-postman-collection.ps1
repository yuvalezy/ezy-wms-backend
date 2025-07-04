# PowerShell Script to Generate Postman Collection for EzyWMS API
# 
# This script downloads the OpenAPI spec and generates a basic Postman collection
# 
# Prerequisites:
# - API server running on localhost:5000
# 
# Usage:
# .\generate-postman-collection.ps1

param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$OutputDir = "..\postman-collections"
)

# Configuration
$SwaggerUrl = "$BaseUrl/swagger/v1/swagger.json"

Write-Host "🚀 Generating Postman collection for EzyWMS API..." -ForegroundColor Green
Write-Host "📡 Base URL: $BaseUrl" -ForegroundColor Yellow

# Create output directory
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

try {
    # Download OpenAPI spec
    Write-Host "📡 Fetching OpenAPI spec from: $SwaggerUrl" -ForegroundColor Yellow
    $openApiSpec = Invoke-RestMethod -Uri $SwaggerUrl -Method Get
    
    Write-Host "✅ OpenAPI spec loaded successfully" -ForegroundColor Green
    Write-Host "📊 Found $($openApiSpec.paths.Count) endpoint paths" -ForegroundColor Cyan
    
    # Generate basic Postman collection structure
    $collection = @{
        info = @{
            name = if ($openApiSpec.info.title) { $openApiSpec.info.title } else { "EzyWMS API" }
            description = if ($openApiSpec.info.description) { $openApiSpec.info.description } else { "ASP.NET Core Web API for EzyWMS warehouse management system" }
            version = if ($openApiSpec.info.version) { $openApiSpec.info.version } else { "1.0.0" }
            schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
        }
        auth = @{
            type = "bearer"
            bearer = @(
                @{
                    key = "token"
                    value = "{{bearerToken}}"
                    type = "string"
                }
            )
        }
        variable = @(
            @{
                key = "baseUrl"
                value = $BaseUrl
                type = "string"
            }
        )
        item = @()
    }
    
    # Add authentication folder
    $authFolder = @{
        name = "🔐 Authentication"
        item = @(
            @{
                name = "Login"
                request = @{
                    method = "POST"
                    header = @(
                        @{
                            key = "Content-Type"
                            value = "application/json"
                        }
                    )
                    body = @{
                        mode = "raw"
                        raw = @"
{
  "password": "{{password}}"
}
"@
                    }
                    url = @{
                        raw = "{{baseUrl}}/api/authentication/login"
                        host = @("{{baseUrl}}")
                        path = @("api", "authentication", "login")
                    }
                    description = "Login and get bearer token"
                }
                event = @(
                    @{
                        listen = "test"
                        script = @{
                            exec = @(
                                "if (pm.response.code === 200) {",
                                "    const response = pm.response.json();",
                                "    if (response.token) {",
                                "        pm.environment.set('bearerToken', response.token);",
                                "        console.log('Bearer token set successfully');",
                                "    }",
                                "}"
                            )
                        }
                    }
                )
            }
            @{
                name = "Get Company Info"
                request = @{
                    method = "GET"
                    header = @()
                    url = @{
                        raw = "{{baseUrl}}/api/authentication/CompanyName"
                        host = @("{{baseUrl}}")
                        path = @("api", "authentication", "CompanyName")
                    }
                    description = "Get company information (no authentication required)"
                }
            }
        )
    }
    
    $collection.item += $authFolder
    
    # Group endpoints by controller
    $controllerGroups = @{}
    
    foreach ($pathKey in $openApiSpec.paths.PSObject.Properties.Name) {
        $pathItem = $openApiSpec.paths.$pathKey
        
        foreach ($method in @('get', 'post', 'put', 'delete', 'patch')) {
            if ($pathItem.PSObject.Properties.Name -contains $method) {
                $operation = $pathItem.$method
                $tag = if ($operation.tags -and $operation.tags.Count -gt 0) { $operation.tags[0] } else { "Default" }
                
                if (-not $controllerGroups.ContainsKey($tag)) {
                    $controllerGroups[$tag] = @{
                        name = $tag
                        item = @()
                    }
                }
                
                # Convert path parameters
                $postmanPath = $pathKey -replace '\{([^}]+)\}', ':$1'
                
                $request = @{
                    name = if ($operation.summary) { $operation.summary } else { "$($method.ToUpper()) $pathKey" }
                    request = @{
                        method = $method.ToUpper()
                        header = @(
                            @{
                                key = "Content-Type"
                                value = "application/json"
                                type = "text"
                            }
                        )
                        url = @{
                            raw = "{{baseUrl}}$postmanPath"
                            host = @("{{baseUrl}}")
                            path = $postmanPath.Split('/') | Where-Object { $_ -ne "" }
                        }
                        description = if ($operation.description) { $operation.description } elseif ($operation.summary) { $operation.summary } else { "" }
                    }
                }
                
                # Add auth for non-anonymous endpoints
                if ($operation.security) {
                    $request.request.auth = @{
                        type = "bearer"
                        bearer = @(
                            @{
                                key = "token"
                                value = "{{bearerToken}}"
                                type = "string"
                            }
                        )
                    }
                }
                
                $controllerGroups[$tag].item += $request
            }
        }
    }
    
    # Add controller groups to collection
    $controllerOrder = @('GoodsReceipt', 'Package', 'Transfer', 'Counting', 'Picking', 'User', 'Authentication', 'Authorization', 'General', 'Service')
    
    foreach ($controllerName in $controllerOrder) {
        if ($controllerGroups.ContainsKey($controllerName)) {
            $collection.item += $controllerGroups[$controllerName]
            $controllerGroups.Remove($controllerName)
        }
    }
    
    # Add remaining controllers
    foreach ($group in $controllerGroups.Values) {
        $collection.item += $group
    }
    
    # Save collection
    $collectionPath = Join-Path $OutputDir "EzyWMS-API-Collection.postman_collection.json"
    $collection | ConvertTo-Json -Depth 10 | Out-File -FilePath $collectionPath -Encoding UTF8
    
    Write-Host "📁 Postman collection saved to: $collectionPath" -ForegroundColor Green
    Write-Host "📋 Collection contains $($collection.item.Count) folders" -ForegroundColor Cyan
    
    # Generate environment
    $environment = @{
        name = "EzyWMS API Environment"
        values = @(
            @{
                key = "baseUrl"
                value = $BaseUrl
                description = "Base URL for the EzyWMS API"
                enabled = $true
            }
            @{
                key = "bearerToken"
                value = ""
                description = "JWT Bearer token for authentication (set automatically after login)"
                enabled = $true
            }
            @{
                key = "password"
                value = "your_password_here"
                description = "Password for login (update with your actual password)"
                enabled = $true
            }
        )
        "_postman_variable_scope" = "environment"
    }
    
    $environmentPath = Join-Path $OutputDir "EzyWMS-API-Environment.postman_environment.json"
    $environment | ConvertTo-Json -Depth 5 | Out-File -FilePath $environmentPath -Encoding UTF8
    
    Write-Host "🌍 Postman environment saved to: $environmentPath" -ForegroundColor Green
    
    # Generate README
    $readmePath = Join-Path $OutputDir "README.md"
    $readmeContent = @"
# EzyWMS API Postman Collection

## Overview
This collection contains comprehensive API documentation and examples for the EzyWMS warehouse management system.

## Files Generated
- ``EzyWMS-API-Collection.postman_collection.json`` - Complete API collection with $($collection.item.Count) endpoint groups
- ``EzyWMS-API-Environment.postman_environment.json`` - Environment variables for API testing

## Setup Instructions

1. **Import Collection**: Import the collection file into Postman
2. **Import Environment**: Import the environment file into Postman  
3. **Set Environment**: Select the "EzyWMS API Environment" in Postman
4. **Update Password**: Set your password in the environment variable ``password``
5. **Login**: Run the "🔐 Authentication → Login" request to get your bearer token
6. **Start Testing**: All authenticated endpoints will automatically use the bearer token

## Authentication
- The collection uses Bearer token authentication
- Login via the Authentication folder to automatically set the token
- The token is automatically applied to all authenticated requests

## API Documentation
- **Base URL**: $BaseUrl
- **Swagger UI**: $BaseUrl/swagger
- **OpenAPI Spec**: $SwaggerUrl

## Generated on
$(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Collection Statistics
- Total Endpoint Paths: $($openApiSpec.paths.Count)
- Total Folders: $($collection.item.Count)
- Authentication: JWT Bearer Token
- Content-Type: application/json
"@
    
    $readmeContent | Out-File -FilePath $readmePath -Encoding UTF8
    Write-Host "📖 README saved to: $readmePath" -ForegroundColor Green
    
    Write-Host "`n🎉 Postman collection generation completed successfully!" -ForegroundColor Green
    Write-Host "`n📋 Next steps:" -ForegroundColor Yellow
    Write-Host "1. Import the collection and environment files into Postman" -ForegroundColor White
    Write-Host "2. Update the password in the environment variables" -ForegroundColor White
    Write-Host "3. Run the Login request to authenticate" -ForegroundColor White
    Write-Host "4. Start testing the API endpoints!" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error generating Postman collection: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Message -like "*connection*" -or $_.Exception.Message -like "*refused*") {
        Write-Host "`n💡 Make sure the API server is running on $BaseUrl" -ForegroundColor Yellow
        Write-Host "   You can start it with: dotnet run --project Service" -ForegroundColor Yellow
    }
    
    exit 1
}