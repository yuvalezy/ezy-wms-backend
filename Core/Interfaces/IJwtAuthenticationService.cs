using System.Security.Claims;
using Core.Entities;

namespace Core.Interfaces;

public interface IJwtAuthenticationService
{
    string GenerateToken(User user, DateTime expiresAt);
    ClaimsPrincipal? ValidateToken(string token);
}