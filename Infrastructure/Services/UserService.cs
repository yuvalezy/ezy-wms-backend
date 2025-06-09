using Core;
using Core.DTOs.Settings;
using Core.Entities;
using Core.Interfaces;
using Core.Utils;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class UserService(SystemDbContext dbContext, IExternalSystemAdapter externalSystem, ILogger<UserService> logger) : IUserService {
    public async Task<IEnumerable<UserResponse>> GetUsersAsync() {
        try {
            return await dbContext.Users
                .Include(u => u.AuthorizationGroup)
                .Select(u => new UserResponse {
                    Id                     = u.Id,
                    FullName               = u.FullName,
                    Email                  = u.Email,
                    Position               = u.Position,
                    SuperUser              = u.SuperUser,
                    Active                 = u.Active,
                    AuthorizationGroupId   = u.AuthorizationGroupId,
                    AuthorizationGroupName = u.AuthorizationGroup != null ? u.AuthorizationGroup.Name : null,
                    Warehouses             = u.Warehouses,
                    ExternalId             = u.ExternalId,
                    CreatedAt              = u.CreatedAt,
                    UpdatedAt              = u.UpdatedAt
                })
                .ToListAsync();
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving users");
            throw;
        }
    }

    public async Task<User?> GetUserAsync(Guid id) {
        try {
            var user = await dbContext.Users
                .Include(u => u.AuthorizationGroup)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user != null) {
                user.Password = "*********"; // Mask password for security
            }

            return user;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving user {UserId}", id);
            throw;
        }
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request) {
        try {
            await ValidateUserRequest(request);

            var newUser = new User {
                Id                   = Guid.NewGuid(),
                FullName             = request.FullName,
                Password             = PasswordUtils.HashPasswordWithSalt(request.Password),
                Email                = request.Email,
                Position             = request.Position,
                SuperUser            = request.SuperUser,
                Active               = true,
                AuthorizationGroupId = request.AuthorizationGroupId,
                ExternalId           = request.ExternalId,
                CreatedAt            = DateTime.UtcNow,
                UpdatedAt            = DateTime.UtcNow,
                Warehouses           = request.Warehouses
            };

            dbContext.Users.Add(newUser);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Created new user {UserId}", newUser.Id);

            newUser.Password = "*********"; // Mask password for security

            return newUser;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error creating user");
            throw;
        }
    }

    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid currentUserId) {
        try {
            var user = await dbContext.Users.FindAsync(id);
            if (user == null || user.Deleted) {
                logger.LogWarning("User {UserId} not found or deleted for update", id);
                return false;
            }

            // Prevent user from removing their own super user status
            if (currentUserId == id && request.SuperUser == false && user.SuperUser) {
                throw new InvalidOperationException("Cannot remove your own super user status.");
            }

            await ValidateUserRequest(request);

            // Update user properties
            user.FullName             = request.FullName;
            user.Email                = request.Email;
            user.Position             = request.Position;
            user.SuperUser            = request.SuperUser;
            user.AuthorizationGroupId = request.AuthorizationGroupId;
            user.ExternalId           = request.ExternalId;
            user.Warehouses           = request.Warehouses;

            // Only update password if provided
            if (!string.IsNullOrWhiteSpace(request.Password))
                user.Password = PasswordUtils.HashPasswordWithSalt(request.Password);

            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            logger.LogInformation("Updated user {UserId}", id);
            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error updating user {UserId}", id);
            throw;
        }
    }

    private async Task ValidateUserRequest(UserRequest request) {
        // Validate authorization group if provided
        if (request.AuthorizationGroupId.HasValue) {
            bool groupExists = await dbContext.AuthorizationGroups.AnyAsync(g => g.Id == request.AuthorizationGroupId.Value);
            if (!groupExists) {
                throw new InvalidOperationException("Authorization group not found.");
            }
        }

        if (request.ExternalId != null) {
            var employee = await externalSystem.GetUserInfoAsync(request.ExternalId);
            if (employee == null) {
                throw new InvalidOperationException("External user not found.");
            }
        }
    }


    public async Task<bool> DeleteUserAsync(Guid id, Guid currentUserId) {
        try {
            // Prevent deletion of default system user
            if (id == Const.DefaultUserId) {
                throw new InvalidOperationException("Cannot delete the default system user.");
            }

            // Prevent user from deleting themselves
            if (currentUserId == id) {
                throw new InvalidOperationException("Cannot delete your own user account.");
            }

            var user = await dbContext.Users.FindAsync(id);
            if (user == null) {
                logger.LogWarning("User {UserId} not found for deletion", id);
                return false;
            }

            user.Deleted   = true;
            user.DeletedAt = DateTime.UtcNow;
            user.Active    = false;

            await dbContext.SaveChangesAsync();

            logger.LogInformation("Deleted user {UserId}", id);
            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error deleting user {UserId}", id);
            throw;
        }
    }

    public async Task<bool> DisableUserAsync(Guid id, Guid currentUserId) {
        try {
            // Prevent user from disabling themselves
            if (currentUserId == id) {
                throw new InvalidOperationException("Cannot disable your own user account.");
            }

            var user = await dbContext.Users.FindAsync(id);
            if (user == null || user.Deleted) {
                logger.LogWarning("User {UserId} not found or deleted for disable", id);
                return false;
            }

            if (!user.Active) {
                throw new InvalidOperationException("User is already disabled.");
            }

            user.Active    = false;
            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            logger.LogInformation("Disabled user {UserId}", id);
            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error disabling user {UserId}", id);
            throw;
        }
    }

    public async Task<bool> EnableUserAsync(Guid id) {
        try {
            var user = await dbContext.Users.FindAsync(id);
            if (user == null || user.Deleted) {
                logger.LogWarning("User {UserId} not found or deleted for enable", id);
                return false;
            }

            if (user.Active) {
                throw new InvalidOperationException("User is already enabled.");
            }

            user.Active    = true;
            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            logger.LogInformation("Enabled user {UserId}", id);
            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error enabling user {UserId}", id);
            throw;
        }
    }
}