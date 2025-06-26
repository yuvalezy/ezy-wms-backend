using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Service.Services;

public class BackgroundPickListSyncService(
    IServiceScopeFactory                    scopeFactory,
    ILogger<BackgroundPickListSyncService>  logger,
    IOptions<BackgroundPickListSyncOptions> options)
    : BackgroundService {
    private readonly BackgroundPickListSyncOptions options       = options.Value;
    private readonly SemaphoreSlim                 syncSemaphore = new(1, 1);
    private          DateTime                      lastSync      = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!options.Enabled) {
            logger.LogInformation("BackgroundPickListSyncService is disabled");
            return;
        }

        logger.LogInformation("BackgroundPickListSyncService started with interval of {IntervalSeconds} seconds", options.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested) {
            try {
                await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await TriggerSync();
            }
            catch (OperationCanceledException) {
                // Expected when cancellation is requested
            }
            catch (Exception ex) {
                logger.LogError(ex, "Error occurred during background pick list sync");
            }
        }
    }

    public async Task TriggerSync() {
        // Ensure only one sync runs at a time
        if (!await syncSemaphore.WaitAsync(0)) {
            logger.LogInformation("Sync already in progress, skipping");
            return;
        }

        try {
            lastSync = DateTime.UtcNow;
            logger.LogInformation("Starting background pick list sync");

            using var scope           = scopeFactory.CreateScope();
            var       pickListService = scope.ServiceProvider.GetRequiredService<IPickListProcessService>();

            await pickListService.SyncPendingPickLists();

            logger.LogInformation("Background pick list sync completed");
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error during pick list sync");
        }
        finally {
            syncSemaphore.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        logger.LogInformation("BackgroundPickListSyncService is stopping");
        await base.StopAsync(cancellationToken);
    }
}

public class BackgroundPickListSyncOptions {
    public int  IntervalSeconds { get; set; } = 60;
    public bool Enabled         { get; set; } = true;
}