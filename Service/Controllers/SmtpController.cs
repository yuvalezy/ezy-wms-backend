using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.DTOs.Settings;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Service.Controllers;

/// <summary>
/// SMTP Configuration Controller - Manages SMTP email settings for alert notifications
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SmtpController(
    ISettings settings,
    IEmailService emailService,
    IUserService userService,
    ILogger<SmtpController> logger) : ControllerBase {

    private async Task<bool> IsUserSuperUser() {
        string? userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) {
            return false;
        }

        var user = await userService.GetUserAsync(userId);
        return user?.SuperUser ?? false;
    }

    /// <summary>
    /// Gets the current SMTP configuration (passwords are masked for security)
    /// </summary>
    /// <returns>Current SMTP settings</returns>
    /// <response code="200">Returns the SMTP settings</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user is not a super user</response>
    /// <response code="500">If a server error occurs</response>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(SmtpSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSettings() {
        try {
            if (!await IsUserSuperUser()) {
                return StatusCode(403, new { error = "forbidden", error_description = "Solo los super usuarios pueden acceder a la configuración SMTP." });
            }

            var response = new SmtpSettingsResponse {
                Enabled = settings.Smtp.Enabled,
                Host = settings.Smtp.Host,
                Port = settings.Smtp.Port,
                EnableSsl = settings.Smtp.EnableSsl,
                Username = settings.Smtp.Username,
                HasPassword = !string.IsNullOrEmpty(settings.Smtp.Password),
                FromEmail = settings.Smtp.FromEmail,
                FromName = settings.Smtp.FromName,
                TimeoutSeconds = settings.Smtp.TimeoutSeconds
            };

            return Ok(response);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving SMTP settings");
            return StatusCode(500, new { error = "server_error", error_description = "Ocurrió un error al obtener la configuración SMTP." });
        }
    }

    /// <summary>
    /// Tests the SMTP connection by sending a test email
    /// </summary>
    /// <param name="request">Test email request containing the recipient email address</param>
    /// <returns>Success or failure message</returns>
    /// <response code="200">Returns success message if test email sent</response>
    /// <response code="400">If the email address is invalid or SMTP is not configured</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user is not a super user</response>
    /// <response code="500">If a server error occurs</response>
    [HttpPost("test")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestConnection([FromBody] TestSmtpRequest request) {
        try {
            if (!await IsUserSuperUser()) {
                return StatusCode(403, new { error = "forbidden", error_description = "Solo los super usuarios pueden probar la configuración SMTP." });
            }

            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            if (!emailService.IsSmtpConfigured()) {
                return BadRequest(new { error = "smtp_not_configured", error_description = "SMTP no está configurado o no está habilitado." });
            }

            bool success = await emailService.TestSmtpConnectionAsync(request.TestEmail);

            if (success) {
                logger.LogInformation("SMTP test email sent successfully to {Email}", request.TestEmail);
                return Ok(new { message = $"Correo de prueba enviado exitosamente a {request.TestEmail}." });
            }
            else {
                logger.LogWarning("SMTP test email failed to {Email}", request.TestEmail);
                return BadRequest(new { error = "test_failed", error_description = "No se pudo enviar el correo de prueba. Verifique la configuración SMTP y los logs para más detalles." });
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error testing SMTP connection");
            return StatusCode(500, new { error = "server_error", error_description = "Ocurrió un error al probar la conexión SMTP." });
        }
    }
}

public class TestSmtpRequest {
    [Required(ErrorMessage = "El email de prueba es requerido")]
    [EmailAddress(ErrorMessage = "El email de prueba no es válido")]
    public required string TestEmail { get; set; }
}
