using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Service.Swagger;

/// <summary>
/// Swagger operation filter to document Device UUID header requirements
/// </summary>
public class DeviceUuidOperationFilter : IOperationFilter {
    private readonly HashSet<string> allowedEndpoints = new(System.StringComparer.OrdinalIgnoreCase) {
        "/api/authentication/login",
        "/api/authentication/logout", 
        "/api/authentication/companyinfo",
        "/api/users",
        "/api/authorization-groups",
        "/api/device",
        "/api/license/status",
        "/swagger",
        "/health"
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context) {
        // Get the relative path from the operation
        var relativePath = context.ApiDescription.RelativePath?.ToLowerInvariant();
        
        // Check if this endpoint requires Device UUID
        if (!string.IsNullOrEmpty(relativePath) && !IsAllowedEndpoint(relativePath)) {
            // Check if endpoint allows anonymous access
            var allowAnonymous = context.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Any() ||
                               context.MethodInfo.DeclaringType?.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Any() == true;

            if (!allowAnonymous) {
                // Add Device UUID parameter
                operation.Parameters ??= new List<OpenApiParameter>();
                
                operation.Parameters.Add(new OpenApiParameter {
                    Name = "X-Device-UUID",
                    In = ParameterLocation.Header,
                    Description = "Device UUID required for license validation. Generate using getOrCreateDeviceUUID() function.",
                    Required = true,
                    Schema = new OpenApiSchema {
                        Type = "string",
                        Format = "uuid"
                    }
                });

                // Add Device UUID security requirement
                operation.Security ??= new List<OpenApiSecurityRequirement>();
                
                var deviceUuidRequirement = new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme {
                            Reference = new OpenApiReference {
                                Type = ReferenceType.SecurityScheme,
                                Id = "DeviceUUID"
                            }
                        },
                        new List<string>()
                    }
                };

                operation.Security.Add(deviceUuidRequirement);

                // Add note to description
                var deviceNote = "\n\n**Device UUID Required:** This endpoint requires the `X-Device-UUID` header for license validation.";
                operation.Description = (operation.Description ?? "") + deviceNote;
            }
        }
    }

    private bool IsAllowedEndpoint(string path) {
        return allowedEndpoints.Any(endpoint => 
            path.StartsWith(endpoint.TrimStart('/'), System.StringComparison.OrdinalIgnoreCase));
    }
}