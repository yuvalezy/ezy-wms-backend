using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Service.Services;

/// <summary>
/// Background service that sends periodic heartbeat signals to Uptime Kuma.
/// Implements push-based monitoring for intranet services.
/// </summary>
public class UptimeKumaHeartbeatService(
    ILogger<UptimeKumaHeartbeatService> logger,
    IConfiguration                      configuration,
    IHttpClientFactory                  httpClientFactory) : BackgroundService {
    private readonly bool     _enabled             = bool.Parse(configuration["UptimeKuma:Enabled"] ?? "false");
    private readonly string   _pushUrl             = configuration["UptimeKuma:PushUrl"] ?? "";
    private readonly TimeSpan _heartbeatInterval   = TimeSpan.FromSeconds(int.Parse(configuration["UptimeKuma:HeartbeatIntervalSeconds"] ?? "60"));
    private readonly bool     _sendOnStartup       = bool.Parse(configuration["UptimeKuma:SendOnStartup"] ?? "true");
    private readonly bool     _sendOnShutdown      = bool.Parse(configuration["UptimeKuma:SendOnShutdown"] ?? "true");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        // Skip if disabled or no push URL configured
        if (!_enabled || string.IsNullOrWhiteSpace(_pushUrl)) {
            if (!_enabled) {
                logger.LogDebug("Uptime Kuma heartbeat service is disabled.");
            }
            else {
                logger.LogWarning("Uptime Kuma heartbeat service enabled but no PushUrl configured.");
            }
            return;
        }

        logger.LogInformation(
            "Uptime Kuma heartbeat service started. Sending heartbeat every {Interval} seconds to {Url}",
            _heartbeatInterval.TotalSeconds,
            MaskUrl(_pushUrl));

        // Send startup heartbeat
        if (_sendOnStartup) {
            await SendHeartbeatAsync("up", "Service started", stoppingToken);
        }

        // Main heartbeat loop
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await Task.Delay(_heartbeatInterval, stoppingToken);
                await SendHeartbeatAsync("up", "OK", stoppingToken);
            }
            catch (TaskCanceledException) {
                // Expected when service is stopping
                break;
            }
            catch (Exception ex) {
                // Log but continue - heartbeat failures should never break the service
                logger.LogWarning(ex, "Error in heartbeat loop, continuing...");
            }
        }

        // Send shutdown heartbeat
        if (_sendOnShutdown) {
            await SendHeartbeatAsync("down", "Service stopping", CancellationToken.None);
        }

        logger.LogInformation("Uptime Kuma heartbeat service stopped.");
    }

    /// <summary>
    /// Sends a heartbeat signal to Uptime Kuma.
    /// </summary>
    /// <param name="status">Status to send (up or down)</param>
    /// <param name="message">Optional message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task SendHeartbeatAsync(string status, string message, CancellationToken cancellationToken) {
        try {
            var httpClient = httpClientFactory.CreateClient();

            // Build URL with status and message parameters
            var url       = _pushUrl;
            var separator = url.Contains('?') ? "&" : "?";
            url += $"{separator}status={Uri.EscapeDataString(status)}&msg={Uri.EscapeDataString(message)}";

            // Send heartbeat (fire and forget, but await for error handling)
            var response = await httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode) {
                logger.LogDebug("Heartbeat sent successfully: status={Status}, msg={Message}", status, message);
            }
            else {
                logger.LogWarning(
                    "Heartbeat request returned non-success status code: {StatusCode}",
                    response.StatusCode);
            }
        }
        catch (OperationCanceledException) {
            // Expected during shutdown, don't log as error
            logger.LogDebug("Heartbeat cancelled during shutdown");
        }
        catch (Exception ex) {
            // CRITICAL: Never propagate exceptions - heartbeat failures must not affect service
            logger.LogWarning(ex, "Failed to send heartbeat to Uptime Kuma (status={Status})", status);
        }
    }

    /// <summary>
    /// Masks sensitive parts of the URL for logging.
    /// </summary>
    private static string MaskUrl(string url) {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        try {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath[..Math.Min(20, uri.AbsolutePath.Length)]}...";
        }
        catch {
            return "[invalid-url]";
        }
    }
}
