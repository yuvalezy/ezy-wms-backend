using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Settings;

public class SmtpSettingsRequest {
    [Required]
    public bool Enabled { get; set; }

    [Required(ErrorMessage = "El host SMTP es requerido")]
    [MaxLength(200)]
    public required string Host { get; set; }

    [Required(ErrorMessage = "El puerto SMTP es requerido")]
    [Range(1, 65535, ErrorMessage = "El puerto debe estar entre 1 y 65535")]
    public int Port { get; set; } = 587;

    [Required]
    public bool EnableSsl { get; set; } = true;

    [MaxLength(200)]
    public string? Username { get; set; }

    [MaxLength(500)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El email de origen es requerido")]
    [EmailAddress(ErrorMessage = "El email de origen no es válido")]
    [MaxLength(200)]
    public required string FromEmail { get; set; }

    [Required(ErrorMessage = "El nombre de origen es requerido")]
    [MaxLength(200)]
    public required string FromName { get; set; } = "EzyWMS";

    [Range(5, 300, ErrorMessage = "El timeout debe estar entre 5 y 300 segundos")]
    public int TimeoutSeconds { get; set; } = 30;

    [Required(ErrorMessage = "El TimeZone ID es requerido")]
    [MaxLength(100)]
    public required string TimeZoneId { get; set; } = "America/Panama";
}
