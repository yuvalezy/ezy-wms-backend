using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Utils;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AuthenticationService(
    SystemDbContext                dbContext,
    IJwtAuthenticationService      jwtService,
    ISessionManager                sessionManager,
    IExternalSystemAdapter         externalSystemAdapter,
    ILogger<AuthenticationService> logger) : IAuthenticationService {
    public async Task<SessionInfo?> LoginAsync(LoginRequest request) {
        try {
            // Find all users and check password
            var users = await dbContext.Users
                .Include(u => u.AuthorizationGroup)
                .ToListAsync();

            var authenticatedUser = users.FirstOrDefault(user => PasswordUtils.VerifyPassword(request.Password, user.Password));

            if (authenticatedUser == null) {
                logger.LogWarning("Login failed: Invalid password");
                return null;
            }

            // Check if user is deleted
            if (authenticatedUser.Deleted) {
                logger.LogWarning("Login failed: User {UserId} is deleted", authenticatedUser.Id);
                return null;
            }

            // Check if user is active
            if (!authenticatedUser.Active) {
                logger.LogWarning("Login failed: User {UserId} is disabled", authenticatedUser.Id);
                return null;
            }

            if (!authenticatedUser.SuperUser && authenticatedUser.Warehouses.Count == 0) {
                logger.LogWarning("Login failed: User {UserId} has no warehouses assigned", authenticatedUser.Id);
                return null;
            }

            if ((authenticatedUser.SuperUser || authenticatedUser.Warehouses.Count > 1) && request.Warehouse == null) {
                //need to return error to the browser with list of warehouses as the user must select a warehouse for login parameter
                var warehouses = externalSystemAdapter.GetWarehousesAsync(authenticatedUser.Warehouses.Count > 0 ? authenticatedUser.Warehouses.ToArray() : null);
            }

            // Generate token
            var expiresAt = DateTime.UtcNow.Date.AddDays(1); // Expires at midnight
            var token     = jwtService.GenerateToken(authenticatedUser, expiresAt);

            var authorizations = authenticatedUser.AuthorizationGroup?.Authorizations ?? new List<Authorization>();

            var sessionInfo = new SessionInfo {
                UserId         = authenticatedUser.Id.ToString(),
                SuperUser      = authenticatedUser.SuperUser,
                Authorizations = authorizations,
                Token          = token,
                ExpiresAt      = expiresAt
            };

            // Store session in memory
            await sessionManager.SetValueAsync(token, sessionInfo.ToJson(), TimeSpan.FromDays(1));

            return sessionInfo;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during login");
            throw;
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword) {
        try {
            var user = await dbContext.Users.FindAsync(userId);
            if (user == null || user.Deleted) {
                logger.LogWarning("User {UserId} not found or deleted for password change", userId);
                return false;
            }

            // Verify current password
            if (!PasswordUtils.VerifyPassword(currentPassword, user.Password)) {
                logger.LogWarning("Invalid current password for user {UserId}", userId);
                return false;
            }

            // Update password
            user.Password  = PasswordUtils.HashPasswordWithSalt(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            logger.LogInformation("Password changed successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error changing password for user {UserId}", userId);
            throw;
        }
    }

    public async Task LogoutAsync(string sessionToken) {
        try {
            // Remove the session from the session manager
            await sessionManager.RemoveAsync(sessionToken);
            logger.LogInformation("Session removed successfully for token");
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during logout");
            throw;
        }
    }
}