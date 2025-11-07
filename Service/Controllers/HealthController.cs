using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers;

/// <summary>
/// Health Check Controller - Provides health check endpoints for monitoring
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase {
    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    /// <response code="200">Service is healthy</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth() {
        return Ok(new HealthResponse {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Detailed health check with version information
    /// </summary>
    /// <returns>Detailed health status</returns>
    /// <response code="200">Service is healthy with details</response>
    [HttpGet("detailed")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DetailedHealthResponse), StatusCodes.Status200OK)]
    public ActionResult<DetailedHealthResponse> GetDetailedHealth() {
        return Ok(new DetailedHealthResponse {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown",
            Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
        });
    }
}

/// <summary>
/// Basic health response
/// </summary>
public class HealthResponse {
    /// <summary>
    /// Current health status
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// UTC timestamp of the health check
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Detailed health response with additional information
/// </summary>
public class DetailedHealthResponse : HealthResponse {
    /// <summary>
    /// Application version
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Service uptime
    /// </summary>
    public TimeSpan Uptime { get; init; }
}
