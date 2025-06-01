using System;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Service;

public static class DependencyInjectionConfig {
    public static IServiceCollection ConfigureServices(this IServiceCollection services, Settings settings, IConfiguration configuration) {
        services.AddHttpContextAccessor();
        
        string? connString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connString)) {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }
        services.AddDbContext<SystemDbContext>(options =>
            options.UseSqlServer(connString));
        
        services.AddSingleton<ISessionManager>(_ => new InMemorySessionManager());
        return services;
    }
}