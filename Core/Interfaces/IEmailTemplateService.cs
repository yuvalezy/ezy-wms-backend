using Core.Entities;

namespace Core.Interfaces;

public interface IEmailTemplateService {
    /// <summary>
    /// Generates an HTML email template for a WMS alert
    /// </summary>
    /// <param name="alert">The alert to generate email content for</param>
    /// <param name="userName">The name of the user receiving the alert</param>
    /// <returns>HTML string for the email body</returns>
    string GenerateAlertEmailHtml(WmsAlert alert, string userName);
}
