using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using System.Text;

namespace Infrastructure.Services;

public class EmailTemplateService : IEmailTemplateService {
    public string GenerateAlertEmailHtml(WmsAlert alert, string userName) {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='es'>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset='UTF-8'>");
        sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.AppendLine("    <title>Alerta de EzyWMS</title>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>");
        sb.AppendLine("    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f4f4; padding: 20px;'>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td align='center'>");
        sb.AppendLine("                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>");

        // Header section
        sb.AppendLine("                    <tr>");
        sb.AppendLine($"                        <td style='background: {GetAlertColor(alert.AlertType)}; color: white; padding: 30px; border-radius: 8px 8px 0 0;'>");
        sb.AppendLine("                            <h1 style='margin: 0; font-size: 24px; font-weight: bold;'>");
        sb.AppendLine($"                                {GetAlertIcon(alert.AlertType)} EzyWMS");
        sb.AppendLine("                            </h1>");
        sb.AppendLine("                        </td>");
        sb.AppendLine("                    </tr>");

        // Content section
        sb.AppendLine("                    <tr>");
        sb.AppendLine("                        <td style='padding: 40px 30px;'>");
        sb.AppendLine($"                            <p style='margin: 0 0 10px 0; color: #666; font-size: 14px;'>Hola {userName},</p>");
        sb.AppendLine($"                            <h2 style='margin: 0 0 20px 0; color: #333; font-size: 20px;'>{alert.Title}</h2>");
        sb.AppendLine($"                            <p style='margin: 0 0 20px 0; color: #555; font-size: 16px; line-height: 1.6;'>{alert.Message}</p>");

        // Action button (if actionUrl is provided)
        if (!string.IsNullOrEmpty(alert.ActionUrl)) {
            sb.AppendLine("                            <div style='margin: 30px 0;'>");
            sb.AppendLine($"                                <a href='#' style='display: inline-block; background-color: {GetAlertColor(alert.AlertType)}; color: white; text-decoration: none; padding: 12px 30px; border-radius: 5px; font-weight: bold; font-size: 14px;'>");
            sb.AppendLine($"                                    {GetActionButtonText(alert.AlertType)}");
            sb.AppendLine("                                </a>");
            sb.AppendLine("                            </div>");
        }

        // Alert details
        sb.AppendLine("                            <div style='background-color: #f8f9fa; border-left: 4px solid #0066cc; padding: 15px; margin: 20px 0; border-radius: 4px;'>");
        sb.AppendLine("                                <h3 style='margin: 0 0 10px 0; color: #333; font-size: 14px; font-weight: bold;'>Detalles de la Alerta:</h3>");
        sb.AppendLine($"                                <p style='margin: 5px 0; color: #666; font-size: 13px;'><strong>Tipo:</strong> {GetAlertTypeDescription(alert.AlertType)}</p>");
        sb.AppendLine($"                                <p style='margin: 5px 0; color: #666; font-size: 13px;'><strong>Fecha:</strong> {alert.CreatedAt:dd/MM/yyyy HH:mm}</p>");
        sb.AppendLine($"                                <p style='margin: 5px 0; color: #666; font-size: 13px;'><strong>Objeto:</strong> {GetObjectTypeDescription(alert.ObjectType)}</p>");
        sb.AppendLine("                            </div>");

        sb.AppendLine("                        </td>");
        sb.AppendLine("                    </tr>");

        // Footer section
        sb.AppendLine("                    <tr>");
        sb.AppendLine("                        <td style='background-color: #f8f9fa; padding: 20px 30px; border-radius: 0 0 8px 8px; border-top: 1px solid #dee2e6;'>");
        sb.AppendLine("                            <p style='margin: 0; color: #6c757d; font-size: 12px; text-align: center;'>");
        sb.AppendLine("                                Este es un correo automático, por favor no responda a este mensaje.<br>");
        sb.AppendLine("                                © EzyWMS - Sistema de Gestión de Almacenes");
        sb.AppendLine("                            </p>");
        sb.AppendLine("                        </td>");
        sb.AppendLine("                    </tr>");

        sb.AppendLine("                </table>");
        sb.AppendLine("            </td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("    </table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string GetAlertColor(WmsAlertType alertType) {
        return alertType switch {
            WmsAlertType.TransferApprovalRequest => "#ffc107", // Amber/Warning
            WmsAlertType.TransferApproved => "#28a745",        // Green/Success
            WmsAlertType.TransferRejected => "#dc3545",        // Red/Danger
            _ => "#0066cc"                                     // Blue/Default
        };
    }

    private static string GetAlertIcon(WmsAlertType alertType) {
        return alertType switch {
            WmsAlertType.TransferApprovalRequest => "⚠️",
            WmsAlertType.TransferApproved => "✅",
            WmsAlertType.TransferRejected => "❌",
            _ => "📧"
        };
    }

    private static string GetActionButtonText(WmsAlertType alertType) {
        return alertType switch {
            WmsAlertType.TransferApprovalRequest => "Ver Solicitud",
            WmsAlertType.TransferApproved => "Ver Transferencia",
            WmsAlertType.TransferRejected => "Ver Detalles",
            _ => "Ver Alerta"
        };
    }

    private static string GetAlertTypeDescription(WmsAlertType alertType) {
        return alertType switch {
            WmsAlertType.TransferApprovalRequest => "Solicitud de Aprobación",
            WmsAlertType.TransferApproved => "Transferencia Aprobada",
            WmsAlertType.TransferRejected => "Transferencia Rechazada",
            _ => "Alerta General"
        };
    }

    private static string GetObjectTypeDescription(WmsAlertObjectType objectType) {
        return objectType switch {
            WmsAlertObjectType.Transfer => "Transferencia",
            _ => objectType.ToString()
        };
    }
}
