using System.Text.Json;
using Adapters.Common.SBO.Models;
using Adapters.CrossPlatform.SBO.Services;
using Core.Enums;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class Alert(SboCompany sboCompany, ILoggerFactory loggerFactory) : IDisposable {
    private readonly ILogger<Alert> logger = loggerFactory.CreateLogger<Alert>();
    public bool ThrowExceptionOnFailure { get; set; } = false;

    public async Task SendDocumentCreationAlert(
        AlertableObjectType type,
        int wmsNumber,
        int docNumber,
        int docEntry,
        string[] recipients) {
        if (recipients.Length == 0) {
            logger.LogDebug("No recipients configured for {ObjectType} alert", type);
            return;
        }

        try {
            logger.LogInformation("Sending alert for {ObjectType} - WMS #{WmsNumber}, SAP Doc #{DocNumber} (Entry: {DocEntry}) to {RecipientCount} users",
                type, wmsNumber, docNumber, docEntry, recipients.Length);

            var (subject, documentTypeName, objectCode) = GetAlertInfo(type);

            var alertData = new {
                Subject = subject,
                RecipientCollection = recipients.Select(r => new { SendInternal = "tYES", UserCode = r }).ToArray(),
                MessageDataColumns = new object[] {
                    new {
                        ColumnName = "Transacción WMS",
                        MessageDataLines = new[] { new { Value = wmsNumber.ToString() } }
                    },
                    new {
                        ColumnName = documentTypeName,
                        Link = "tYES",
                        MessageDataLines = new[] { new { Value = docNumber.ToString(), Object = objectCode, ObjectKey = docEntry.ToString() } }
                    }
                }
            };

            var (success, errorMessage) = await sboCompany.PostAsync("Messages", alertData);

            if (success) {
                logger.LogInformation("Successfully sent {ObjectType} alert to {RecipientCount} users", type, recipients.Length);
                return;
            }

            logger.LogWarning("Failed to send {ObjectType} alert: {ErrorMessage}", type, errorMessage);
            if (ThrowExceptionOnFailure) {
                throw new Exception(errorMessage);
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Exception while sending {ObjectType} alert", type);
            if (ThrowExceptionOnFailure) {
                throw;
            }
        }
    }

    private (string Subject, string DocumentTypeName, string ObjectCode) GetAlertInfo(AlertableObjectType type) {
        return type switch {
            AlertableObjectType.Transfer => (
                "Transferencia de Inventario Creada desde WMS",
                "Transferencia de Inventario",
                "67"
            ),
            AlertableObjectType.GoodsReceipt => (
                "Entrada de Mercancías Creada desde WMS",
                "Entrada de Mercancías",
                "20"
            ),
            AlertableObjectType.InventoryCounting => (
                "Conteo de Inventario Creado desde WMS",
                "Conteo de Inventario",
                "1470000065"
            ),
            AlertableObjectType.PickList => (
                "Lista de Picking Procesada desde WMS",
                "Lista de Picking",
                "156"
            ),
            AlertableObjectType.ConfirmationAdjustmentsEntry => (
                "Ajustes de Inventario Creados desde WMS",
                "Ajustes de Inventario",
                "59"
            ),
            AlertableObjectType.ConfirmationAdjustmentsExit => (
                "Ajustes de Inventario Creados desde WMS",
                "Ajustes de Inventario",
                "60"
            ),
            AlertableObjectType.PickListCancellation => (
                "Lista de Picking Cancelada desde WMS",
                "Transferencia de Cancelación",
                "156"
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown alert type")
        };
    }

    public void Dispose() {
    }
}