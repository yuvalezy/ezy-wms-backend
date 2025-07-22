using System;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Core.Services;
using Infrastructure.Auth;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Infrastructure.SessionManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service.Services;

namespace Service.Configuration;

public static class DependencyInjectionConfig {
    public static IServiceCollection ConfigureServices(this IServiceCollection services, Settings settings, IConfiguration configuration) {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

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
        services.AddScoped<ITransferPackageService, TransferPackageService>();
        services.AddScoped<ITransferValidationService, TransferValidationService>();
        services.AddScoped<IPickListService, PickListService>();
        services.AddScoped<PickListDetailService>();
        services.AddScoped<IPickListPackageService, PickListPackageService>();
        services.AddScoped<PickListPackageEligibilityService>();
        services.AddScoped<IPickListLineService, PickListLineService>();
        services.AddScoped<IPickListProcessService, PickListProcessService>();
        services.AddScoped<IPickListCheckService, PickListCheckService>();
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        services.AddScoped<IGoodsReceiptReportService, GoodsReceiptReportService>();
        services.AddScoped<IGoodsReceiptLineService, GoodsReceiptLineService>();
        services.AddScoped<IGoodsReceiptLineItemProcessService, GoodsReceiptLineItemProcessService>();
        services.AddScoped<IInventoryCountingsService, InventoryCountingsService>();
        services.AddScoped<IInventoryCountingsLineService, InventoryCountingsLineService>();
        services.AddScoped<ICancellationReasonService, CancellationReasonService>();
        services.AddScoped<IAuthorizationGroupService, AuthorizationGroupService>();

        // Package Management Services
        services.AddScoped<IPackageContentService, PackageContentService>();
        services.AddScoped<IPackageValidationService, PackageValidationService>();
        services.AddScoped<IPackageLocationService, PackageLocationService>();
        services.AddScoped<IPackageService, PackageService>();

        // Device Management Services
        services.AddScoped<IDeviceService, DeviceService>();
        
        // License Management Services
        services.AddScoped<ILicenseEncryptionService, LicenseEncryptionService>();
        services.AddScoped<IAccountStatusService, AccountStatusService>();
        services.AddScoped<ILicenseCacheService, LicenseCacheService>();
        services.AddScoped<ILicenseValidationService, LicenseValidationService>();
        services.AddScoped<ICloudLicenseService, CloudLicenseService>();
        
        // Configure HTTP client for cloud services
        services.AddHttpClient<CloudLicenseService>((serviceProvider, httpClient) => {
            var settingsService = serviceProvider.GetRequiredService<ISettings>();
            var bearerToken = settingsService.Licensing.BearerToken ?? 
                throw new InvalidOperationException("Bearer token not configured");
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "WMS-License-Client/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        });

        // Configure BackgroundPickListSyncService
        services.Configure<BackgroundPickListSyncOptions>(options => {
            options.IntervalSeconds = settings.BackgroundServices.PickListSync.IntervalSeconds;
            options.Enabled = settings.BackgroundServices.PickListSync.Enabled;
        });
        services.AddSingleton<BackgroundPickListSyncService>();
        services.AddHostedService<BackgroundPickListSyncService>(provider => 
            provider.GetRequiredService<BackgroundPickListSyncService>());

        // Configure CloudSyncBackgroundService
        services.Configure<CloudSyncBackgroundOptions>(options => {
            options.SyncIntervalMinutes = settings.BackgroundServices.CloudSync.SyncIntervalMinutes;
            options.ValidationIntervalHours = settings.BackgroundServices.CloudSync.ValidationIntervalHours;
            options.Enabled = settings.BackgroundServices.CloudSync.Enabled;
        });
        services.AddSingleton<CloudSyncBackgroundService>();
        services.AddHostedService<CloudSyncBackgroundService>(provider => 
            provider.GetRequiredService<CloudSyncBackgroundService>());

        switch (settings.ExternalAdapter) {
            // case ExternalAdapterType.SboWindows:
            //     SboWindowsDependencyInjection.ConfigureServices(services);
            //     break;
            case ExternalAdapterType.SboServiceLayer:
                SboServiceLayerDependencyInjection.ConfigureServices(services);
                break;
            default:
                throw new ArgumentOutOfRangeException($"External Adapter {settings.ExternalAdapter} is not supported");
        }

        return services;
    }
}