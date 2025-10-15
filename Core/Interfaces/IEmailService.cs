namespace Core.Interfaces;

public interface IEmailService {
    /// <summary>
    /// Sends an email asynchronously
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML body of the email</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody);

    /// <summary>
    /// Tests the SMTP connection and sends a test email
    /// </summary>
    /// <param name="testEmailAddress">Email address to send test email to</param>
    /// <returns>True if connection test and email sending succeeded</returns>
    Task<bool> TestSmtpConnectionAsync(string testEmailAddress);

    /// <summary>
    /// Checks if SMTP is configured and enabled
    /// </summary>
    /// <returns>True if SMTP is ready to send emails</returns>
    bool IsSmtpConfigured();
}
