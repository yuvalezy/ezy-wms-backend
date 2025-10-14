using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Auth;

/// <summary>
/// Provides user ID extraction from JWT claims for SignalR connections
/// </summary>
public class JwtUserIdProvider : IUserIdProvider {
    public string? GetUserId(HubConnectionContext connection) {
        // Try to get UserId from custom claim first, then fall back to NameIdentifier
        return connection.User?.FindFirst("UserId")?.Value
               ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
