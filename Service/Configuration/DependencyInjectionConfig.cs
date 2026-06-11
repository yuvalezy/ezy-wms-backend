using System;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Core.Services;
using Infrastructure.Auth;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Infrastructure.SessionManager;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Service.Services;
using System.Net.Http;

namespace Service.Configuration;

public static class DependencyInjectionConfig {
    public static IServiceCollection ConfigureServices(this IServiceCollection services, Settings settings, IConfiguration configuration, IHostEnvironment environment) {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        string? connString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connString)) {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<SystemDbContext>(options =>
        options.UseSqlServer(connString));

        switch (settings.SessionManagement.Type) {
            case SessionManagementType.InMemory:
                services.AddSingleton<ISessionManager, InMemorySessionManager>();
                break;
            case SessionManagementType.Redis:
                services.AddSingleton<ISessionManager, RedisSessionManager>();
                break;
            case SessionManagementType.Sql:
                services.AddSingleton<ISessionManager, SqlSessionManager>();
                break;
            default:
                throw new InvalidOperationException($"Unsupported session management type: {settings.SessionManagement.Type}");
        }

        services.AddSingleton<IJwtAuthenticationService, JwtAuthenticationService>();

        // Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPublicService, PublicService>();
        services.AddScoped<ITransferDocumentService, TransferDocumentService>();
        services.AddScoped<ITransferContentService, TransferContentService>();
        services.AddScoped<ITransferProcessingService, TransferProcessingService>();
        services.AddScoped<ITransferLineService, TransferLineService>();
        services.AddScoped<ITransferValidationService, TransferValidationService>();
        services.AddScoped<IDirectTransferService, DirectTransferService>();
        services.AddSingleton<IPickPathSequencer, PickPathSequencer>();
        services.AddScoped<IPickListService, PickListService>();
        services.AddScoped<IPickListDetailService, PickListDetailService>();
        services.AddScoped<IPickListValidationService, PickListValidationService>();
        services.AddScoped<IPickListLineService, PickListLineService>();
        services.AddScoped<IPickingPackageLabelService, PickingPackageLabelService>();
        services.AddScoped<IPickingRepackService, PickingRepackService>();
        services.AddScoped<IPickListProcessService, PickListProcessService>();
        services.AddScoped<IPickListCancelService, PickListCancelService>();
        services.AddScoped<IPickListCheckService, PickListCheckService>();
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        services.AddScoped<IGoodsReceiptReportService, GoodsReceiptReportService>();
        services.AddScoped<IGoodsReceiptLineService, GoodsReceiptLineService>();
        services.AddScoped<IGoodsReceiptLineItemProcessService, GoodsReceiptLineItemProcessService>();
        services.AddScoped<IInventoryCountingsService, InventoryCountingsService>();
        services.AddScoped<IInventoryCountingsLineService, InventoryCountingsLineService>();
        services.AddScoped<ICancellationReasonService, CancellationReasonService>();
        services.AddScoped<IAuthorizationGroupService, AuthorizationGroupService>();
        services.AddScoped<IExternalSystemAlertService, ExternalSystemAlertService>();
        services.AddScoped<IWmsAlertService, WmsAlertService>();
        services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();

        // Email Services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        services.AddScoped<IExternalCommandService, ExternalCommandService>();
        services.AddScoped<IFileDeliveryService, FileDeliveryService>();

        // Item Management Services
        services.AddScoped<IItemService, ItemService>();

        // Device Management Services
        services.AddScoped<IDeviceService, DeviceService>();

        // Post Processing Services
        services.AddSingleton<IPickingPostProcessorFactory, PickingPostProcessorFactory>();

        // License Management Services
        services.AddScoped<ILicenseEncryptionService, LicenseEncryptionService>();
        services.AddScoped<IAccountStatusService, AccountStatusService>();
        services.AddScoped<ILicenseCacheService, LicenseCacheService>();
        services.AddScoped<ILicenseValidationService, LicenseValidationService>();

        // Configure HTTP client for cloud services
        var httpClientBuilder = services.AddHttpClient<ICloudLicenseService, CloudLicenseService>((serviceProvider, httpClient) =>
        {
            var settingsService = serviceProvider.GetRequiredService<ISettings>();
            var bearerToken = settingsService.Licensing.BearerToken ??
                              throw new InvalidOperationException("Bearer token not configured");

            httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

            httpClient.DefaultRequestHeaders.Add("User-Agent", "WMS-License-Client/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        });

        if (environment.IsDevelopment()) {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        }

        // Configure BackgroundPickListSyncService
        services.Configure<BackgroundPickListSyncOptions>(options =>
        {
            options.IntervalSeconds = settings.BackgroundServices.PickListSync.IntervalSeconds;
            options.Enabled = settings.BackgroundServices.PickListSync.Enabled;
        });

        services.AddSingleton<BackgroundPickListSyncService>();
        services.AddHostedService<BackgroundPickListSyncService>(provider =>
        provider.GetRequiredService<BackgroundPickListSyncService>());

        // Configure CloudSyncBackgroundService
        services.Configure<CloudSyncBackgroundOptions>(options =>
        {
            options.SyncIntervalMinutes = settings.BackgroundServices.CloudSync.SyncIntervalMinutes;
            options.ValidationIntervalHours = settings.BackgroundServices.CloudSync.ValidationIntervalHours;
            options.Enabled = settings.BackgroundServices.CloudSync.Enabled;
        });

        services.AddSingleton<CloudSyncBackgroundService>();
        services.AddHostedService<CloudSyncBackgroundService>(provider => provider.GetRequiredService<CloudSyncBackgroundService>());

        // Configure SignalR for real-time notifications
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, JwtUserIdProvider>();

        // Configure Presence Tracking
        if (settings.PresenceTracking.Type == SessionManagementType.InMemory)
            services.AddSingleton<IPresenceTracker, InMemoryPresenceTracker>();
        else
            services.AddSingleton<IPresenceTracker, RedisPresenceTracker>();

        switch (settings.ExternalAdapter) {
            case ExternalAdapterType.SboServiceLayer:
                SboServiceLayerDependencyInjection.ConfigureServices(services);
                break;
            default:
                throw new ArgumentOutOfRangeException($"External Adapter {settings.ExternalAdapter} is not supported");
        }

        return services;
    }
}
