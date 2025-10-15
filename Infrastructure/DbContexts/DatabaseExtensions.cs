using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Core.Utils;

namespace Infrastructure.DbContexts;

public static class DatabaseExtensions {
    public static Guid SystemUserId { get; private set; } = Guid.Empty;
    public static void EnsureDatabaseCreated(this IServiceProvider services) {
        using var scope   = services.CreateScope();
        var       context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();

        // Apply migrations (this will create database if it doesn't exist)
        context.Database.Migrate();

        // Seed default admin user
        SeedDefaultAdminUser(context);
        // Seed system user for background operations
        SeedSystemUser(context);
    }

    private static void SeedDefaultAdminUser(SystemDbContext context) {
        const string email = "admin@localhost";
        
        // Check if user exists
        if (context.Users.Any()) {
            return;
        }

        // Create default admin user
        var defaultAdmin = new User {
            FullName             = "Administrator",
            Password             = PasswordUtils.HashPasswordWithSalt("ezy123"),
            Email                = email,
            Position             = "System Administrator",
            SuperUser            = true,
            Active               = true,
            AuthorizationGroupId = null,
            CreatedAt            = DateTime.UtcNow,
            UpdatedAt            = DateTime.UtcNow
        };

        context.Users.Add(defaultAdmin);
        context.SaveChanges();
    }

    private static void SeedSystemUser(SystemDbContext context) {
        const string email = "system@localhost";
        // Check if system user already exists - ignore the global query filter
        var user  = context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Email == email && u.Deleted);
        if (user != null) {
            SystemUserId = user.Id;
            return;
        }

        // Create system user for background operations
        var systemUser = new User {
            FullName             = "System Background Service",
            Password             = PasswordUtils.HashPasswordWithSalt(Guid.NewGuid().ToString()), // Random password, not used for login
            Email                = email,
            Position             = "Background Service",
            SuperUser            = false,
            Active               = false, // Not active for regular login
            Deleted              = true,  // Marked as deleted to prevent normal usage
            AuthorizationGroupId = null,
            CreatedAt            = DateTime.UtcNow,
            UpdatedAt            = DateTime.UtcNow
        };

        context.Users.Add(systemUser);
        context.SaveChanges();
        SystemUserId = systemUser.Id;
    }
}