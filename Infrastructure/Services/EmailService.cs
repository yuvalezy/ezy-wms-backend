using Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Infrastructure.Services;

public class EmailService(
    ISettings settings,
    ILogger<EmailService> logger) : IEmailService {

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody) {
        if (!IsSmtpConfigured()) {
            logger.LogWarning("SMTP is not configured or not enabled. Cannot send email.");
            return false;
        }

        try {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.Smtp.FromName, settings.Smtp.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Set timeout
            client.Timeout = settings.Smtp.TimeoutSeconds * 1000;

            // Connect to SMTP server
            var secureSocketOptions = settings.Smtp.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(settings.Smtp.Host, settings.Smtp.Port, secureSocketOptions);

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(settings.Smtp.Username) && !string.IsNullOrEmpty(settings.Smtp.Password)) {
                await client.AuthenticateAsync(settings.Smtp.Username, settings.Smtp.Password);
            }

            // Send the email
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'", to, subject);
            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'", to, subject);
            return false;
        }
    }

    public async Task<bool> TestSmtpConnectionAsync(string testEmailAddress) {
        if (!IsSmtpConfigured()) {
            logger.LogWarning("SMTP is not configured or not enabled. Cannot test connection.");
            return false;
        }

        try {
            var testSubject = "Prueba de Conexión SMTP - EzyWMS";
            var testBody = @"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f8f9fa; border-radius: 8px;'>
                        <h2 style='color: #0066cc; border-bottom: 2px solid #0066cc; padding-bottom: 10px;'>
                            Prueba de Conexión SMTP
                        </h2>
                        <p>Este es un correo electrónico de prueba para verificar la configuración SMTP de EzyWMS.</p>
                        <p>Si recibiste este mensaje, la configuración SMTP está funcionando correctamente.</p>
                        <hr style='border: 1px solid #dee2e6; margin: 20px 0;'>
                        <p style='font-size: 0.9em; color: #6c757d;'>
                            Enviado desde EzyWMS - Sistema de Gestión de Almacenes
                        </p>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(testEmailAddress, testSubject, testBody);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to test SMTP connection");
            return false;
        }
    }

    public bool IsSmtpConfigured() {
        return settings.Smtp.Enabled
            && !string.IsNullOrEmpty(settings.Smtp.Host)
            && !string.IsNullOrEmpty(settings.Smtp.FromEmail)
            && settings.Smtp.Port > 0;
    }
}
