using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Service.Swagger;

/// <summary>
/// Swagger operation filter to document authorization requirements
/// </summary>
public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = false;
        var authorizationRequirements = new List<string>();

        // Check for [Authorize] attribute on action
        var authorizeAttributes = context.MethodInfo.GetCustomAttributes<AuthorizeAttribute>();
        if (authorizeAttributes.Any())
        {
            hasAuthorize = true;
            foreach (var attr in authorizeAttributes)
            {
                if (!string.IsNullOrEmpty(attr.Policy))
                {
                    authorizationRequirements.Add($"Policy: {attr.Policy}");
                }
                if (!string.IsNullOrEmpty(attr.Roles))
                {
                    authorizationRequirements.Add($"Roles: {attr.Roles}");
                }
            }
        }

        // Check for [Authorize] attribute on controller
        var controllerAuthorizeAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>();
        if (controllerAuthorizeAttributes?.Any() == true)
        {
            hasAuthorize = true;
            foreach (var attr in controllerAuthorizeAttributes)
            {
                if (!string.IsNullOrEmpty(attr.Policy))
                {
                    authorizationRequirements.Add($"Policy: {attr.Policy}");
                }
                if (!string.IsNullOrEmpty(attr.Roles))
                {
                    authorizationRequirements.Add($"Roles: {attr.Roles}");
                }
            }
        }

        // Check for [RequireAnyRole] attribute
        var requireAnyRoleAttributes = context.MethodInfo.GetCustomAttributes<RequireAnyRoleAttribute>();
        if (requireAnyRoleAttributes.Any())
        {
            hasAuthorize = true;
            foreach (var attr in requireAnyRoleAttributes)
            {
                var roles = string.Join(", ", attr.Roles);
                authorizationRequirements.Add($"Any of these roles: {roles}");
            }
        }

        // Check for [AllowAnonymous] attribute
        var allowAnonymousAttributes = context.MethodInfo.GetCustomAttributes<AllowAnonymousAttribute>();
        if (allowAnonymousAttributes.Any())
        {
            hasAuthorize = false;
            authorizationRequirements.Clear();
            authorizationRequirements.Add("Anonymous access allowed");
        }

        if (hasAuthorize)
        {
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new List<string>()
                    }
                }
            };

            // Add authorization requirements to description
            if (authorizationRequirements.Any())
            {
                var authDescription = $"\n\n**Authorization Requirements:**\n{string.Join("\n", authorizationRequirements.Select(r => $"- {r}"))}";
                operation.Description = (operation.Description ?? "") + authDescription;
            }
        }
        else if (authorizationRequirements.Any())
        {
            // Anonymous access
            var authDescription = $"\n\n**Authorization:** {string.Join(", ", authorizationRequirements)}";
            operation.Description = (operation.Description ?? "") + authDescription;
        }
    }
}