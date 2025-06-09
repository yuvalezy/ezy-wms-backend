using Core;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Core.Utils;

namespace Infrastructure.DbContexts;

public static class DatabaseExtensions {
    public static void EnsureDatabaseCreated(this IServiceProvider services) {
        using var scope   = services.CreateScope();
        var       context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();

        // Apply migrations (this will create database if it doesn't exist)
        context.Database.Migrate();

        // Seed default admin user
        SeedDefaultAdminUser(context);
    }

    private static void SeedDefaultAdminUser(SystemDbContext context) {
        // Check if default admin user already exists
        if (context.Users.Any()) {
            return;
        }

        // Create default admin user
        var defaultAdmin = new User {
            Id                   = Const.DefaultUserId,
            FullName             = "Administrator",
            Password             = PasswordUtils.HashPasswordWithSalt("ezy123"),
            Email                = "admin@localhost",
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
}