using Core.DTOs.General;
using Core.DTOs.Items;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
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
    IDeviceService                 deviceService,
    ILogger<AuthenticationService> logger) : IAuthenticationService {
    public async Task<SessionInfo?> LoginAsync(LoginRequest request, string deviceUuid) {
        try {
            //Validate the device exists
            var  device         = await deviceService.GetDeviceAsync(deviceUuid);
            bool registerDevice = false;
            if (device == null) {
                if (string.IsNullOrWhiteSpace(request.NewDeviceName)) {
                    throw new DeviceRegistrationException("NEW_DEVICE_NAME");
                }

                if (await deviceService.ValidateDeviceNameAvailable(request.NewDeviceName)) {
                    throw new DeviceRegistrationException("NEW_DEVICE_TAKEN");
                }

                registerDevice = true;
            } 

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

            if (authenticatedUser is { SuperUser: false, Warehouses.Count: 0 }) {
                logger.LogWarning("Login failed: User {UserId} has no warehouses assigned", authenticatedUser.Id);
                return null;
            }

            if (!authenticatedUser.SuperUser && !registerDevice && device != null && device.Status != DeviceStatus.Active) {
                logger.LogWarning("Login failed: Device {DeviceUuid} is not active", deviceUuid);
                return null;
            }


            // Handle warehouse selection
            string?            selectedWarehouse = null;
            WarehouseResponse? warehouse         = null;
            if (authenticatedUser.Warehouses.Count == 1) {
                selectedWarehouse = authenticatedUser.Warehouses.First();

                //Validate that the warehouse exists in the system
                warehouse = await externalSystemAdapter.GetWarehouseAsync(selectedWarehouse);
                if (warehouse == null) {
                    logger.LogWarning("Login failed: Warehouse {Warehouse} does not exist", selectedWarehouse);
                    return null;
                }
            }
            else if (authenticatedUser.SuperUser || authenticatedUser.Warehouses.Count > 1) {
                if (string.IsNullOrEmpty(request.Warehouse)) {
                    // Fetch available warehouses
                    string[]? filter     = authenticatedUser.Warehouses.Count > 0 ? authenticatedUser.Warehouses.ToArray() : null;
                    var       warehouses = await externalSystemAdapter.GetWarehousesAsync(filter);
                    throw new WarehouseSelectionRequiredException(warehouses);
                }

                // Validate the provided warehouse
                if (!authenticatedUser.SuperUser && !authenticatedUser.Warehouses.Contains(request.Warehouse)) {
                    logger.LogWarning("Login failed: User {UserId} does not have access to warehouse {Warehouse}",
                        authenticatedUser.Id, request.Warehouse);
                    return null;
                }

                selectedWarehouse = request.Warehouse;
                //Validate that the warehouse exists in the system
                warehouse = await externalSystemAdapter.GetWarehouseAsync(selectedWarehouse);
                if (warehouse == null) {
                    logger.LogWarning("Login failed: Warehouse {Warehouse} does not exist", selectedWarehouse);
                    return null;
                }
            }

            // Generate token
            var    expiresAt = DateTime.UtcNow.Date.AddDays(1); // Expires at midnight
            string token     = jwtService.GenerateToken(authenticatedUser, expiresAt);

            var authorizations = authenticatedUser.AuthorizationGroup?.Authorizations ?? new List<RoleType>();
            if (authenticatedUser.SuperUser) {
                authorizations = Enum.GetValues<RoleType>();
            }

            var sessionInfo = new SessionInfo {
                UserId             = authenticatedUser.Id.ToString(),
                Name               = authenticatedUser.FullName,
                SuperUser          = authenticatedUser.SuperUser,
                Roles              = authorizations,
                Warehouse          = selectedWarehouse!,
                EnableBinLocations = warehouse!.EnableBinLocations,
                DefaultBinLocation = warehouse!.DefaultBinLocation,
                Token              = token,
                ExpiresAt          = expiresAt,
                DeviceUuid         = deviceUuid,
            };

            if (registerDevice) {
                device = await deviceService.RegisterDeviceAsync(deviceUuid, request.NewDeviceName!, sessionInfo);
            }

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