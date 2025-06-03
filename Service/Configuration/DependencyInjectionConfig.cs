using System;
using Adapters.Windows.SBO;
using Adapters.Windows.SBO.Repositories;
using Adapters.Windows.SBO.Services;
using Adapters.Windows.SBO.Utils;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Infrastructure;
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
        services.AddScoped<IInventoryCountingsService, InventoryCountingsService>();
        services.AddScoped<ICancellationReasonService, CancellationReasonService>();
        services.AddScoped<IAuthorizationGroupService, AuthorizationGroupService>();

        // External System Adapters for SBO 9.0
        SboAssembly.RedirectAssembly();
        services.AddSingleton<SboCompany>();
        services.AddScoped<SboDatabaseService>();
        services.AddScoped<SboEmployeeRepository>();
        services.AddScoped<SboGeneralRepository>();
        services.AddScoped<SboItemRepository>();
        services.AddScoped<IExternalSystemAdapter, SboAdapter>();

        return services;
    }
}