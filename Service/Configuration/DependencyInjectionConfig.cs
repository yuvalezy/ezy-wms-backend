using System;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Infrastructure.Auth;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Infrastructure.SessionManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        if (settings.SessionManagement.Type == SessionManagementType.InMemory)
            services.AddSingleton<ISessionManager, InMemorySessionManager>();
        else
            services.AddSingleton<ISessionManager, RedisSessionManager>();

        services.AddSingleton<IJwtAuthenticationService, JwtAuthenticationService>();

        // Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPublicService, PublicService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<ITransferLineService, TransferLineService>();
        services.AddScoped<IPickListService, PickListService>();
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        services.AddScoped<IGoodsReceiptLinesService, GoodsReceiptLinesService>();
        services.AddScoped<IGoodsReceiptAddItemService, GoodsReceiptAddItemService>();
        services.AddScoped<IInventoryCountingsService, InventoryCountingsService>();
        services.AddScoped<ICancellationReasonService, CancellationReasonService>();
        services.AddScoped<IAuthorizationGroupService, AuthorizationGroupService>();

        switch (settings.ExternalAdapter) {
            case ExternalAdapterType.SboWindows:
                SboWindowsDependencyInjection.ConfigureServices(services);
                break;
            case ExternalAdapterType.SboServiceLayer:
                SboServiceLayerDependencyInjection.ConfigureServices(services);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return services;
    }
}