using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Service.Shared;

namespace Service;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRolePermissionAttribute : AuthorizeAttribute {
    public RequireRolePermissionAttribute(Authorization role) {
        Role          = role;
        Policy        = $"{role}";
    }

    public Authorization        Role          { get; }
}
public class RequireSuperUserAttribute : AuthorizeAttribute {
    public RequireSuperUserAttribute() => Policy = "SuperUserOnly";
}
public class RolePermissionHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<RolePermissionRequirement> {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolePermissionRequirement requirement) {
        // Get the HttpContext
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) {
            return Task.CompletedTask;
        }

        // Get the SessionData from HttpContext.Items
        if (!httpContext.Items.TryGetValue("SessionData", out object? sessionDataObj) || sessionDataObj is not SessionInfo sessionInfo) {
            return Task.CompletedTask;
        }

        // Super users have access to everything
        if (sessionInfo.SuperUser) {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if the user has the required permission
        if (sessionInfo.Permissions.Any(p => p == requirement.Role)) {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
public class RolePermissionRequirement(Authorization role) : IAuthorizationRequirement {
    public Authorization        Role          { get; } = role;
}
public class SessionInfo {
    public required string                      UserId      { get; set; }
    public required string                      AccountId   { get; set; }
    public          bool                        SuperUser   { get; set; }
    public required ICollection<Authorization> Permissions { get; set; }

    public const string SessionCookieName = "ezywms_session";
}
public class SuperUserHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<SuperUserRequirement> {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SuperUserRequirement requirement) {
        // Get the HttpContext
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) {
            return Task.CompletedTask;
        }

        // Get the SessionData from HttpContext.Items
        if (!httpContext.Items.TryGetValue("SessionData", out object? sessionDataObj) || sessionDataObj is not SessionInfo sessionInfo) {
            return Task.CompletedTask;
        }

        // Check if the user is a super user
        if (sessionInfo.SuperUser) {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
public class SuperUserRequirement : IAuthorizationRequirement;
public class InMemorySessionManager : ISessionManager {
    private readonly ConcurrentDictionary<string, (string SessionData, DateTime Expiration)> sessions = new();

    public Task SetValueAsync(string token, string sessionData, TimeSpan expiration) {
        sessions[token] = (sessionData, DateTime.UtcNow.Add(expiration));
        return Task.CompletedTask;
    }

    public Task<string?> GetStringAsync(string token) {
        if (sessions.TryGetValue(token, out var session) && session.Expiration > DateTime.UtcNow) {
            sessions[token] = (session.SessionData, DateTime.UtcNow.AddHours(8));
            return Task.FromResult(session.SessionData);
        }

        // Remove expired session
        sessions.TryRemove(token, out _);
        return Task.FromResult<string?>(null);
    }

    public Task<SessionInfo?> GetSessionAsync(string token) {
        string? rawPayload = GetStringAsync(token)?.Result;
        return rawPayload != null
            ? Task.FromResult(JsonUtils.Deserialize<SessionInfo>(rawPayload))
            : Task.FromResult<SessionInfo?>(null);
    }

    public Task RemoveAsync(string token) {
        sessions.TryRemove(token, out _);
        return Task.CompletedTask;
    }
}
public static class JsonUtils {
    private static readonly JsonSerializerOptions Options = new() {
        Converters                  = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
    };

    public static T? Deserialize<T>(string jsonData) => JsonSerializer.Deserialize<T>(jsonData, Options);
}
