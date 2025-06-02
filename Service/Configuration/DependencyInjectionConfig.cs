using System;
using Adapters.Windows.SBO;
using Adapters.Windows.SBO.Repositories;
using Adapters.Windows.SBO.Services;
using Core.Interfaces;
using Core.Models.Settings;
using Infrastructure;
using Infrastructure.Auth;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service.Middlewares;

namespace Service.Configuration;

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
        services.AddSingleton<IJwtAuthenticationService, JwtAuthenticationService>();
        
        // Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        
        // External System Adapters
        services.AddScoped<SapBusinessOneDatabaseService>();
        services.AddScoped<SapEmployeeRepository>();
        services.AddScoped<IExternalSystemAdapter, SapBusinessOneAdapter>();
        
        return services;
    }
}