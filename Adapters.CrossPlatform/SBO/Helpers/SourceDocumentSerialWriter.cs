using Adapters.CrossPlatform.SBO.Models;
using Adapters.CrossPlatform.SBO.Services;
using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

/// <summary>
/// Writes the package-label codes onto the source document (sales order / inventory transfer request)
/// as the line serial number. This is the only thing that mutates the source-document SerialNum field —
/// it touches no quantities or bin allocations, so it is safe to call repeatedly (e.g. to reset serials
/// when a repack is restarted, or to re-push new pack codes for an already-synced pick).
/// </summary>
public static class SourceDocumentSerialWriter {
    private sealed record SourceDocumentMapping(string EndpointName, string LineCollectionName, string SerialFieldName);

    public static async Task WriteAsync(
        SboCompany sboCompany,
        ILogger logger,
        int absEntry,
        IEnumerable<PickList> rows,
        IEnumerable<PickListSboLine> pickLines,
        string? emptyLineSerial = null) {
        var updates = PickingSourceSerialNumberBuilder.Build(rows, pickLines, emptyLineSerial);
        if (updates.Count == 0) {
            logger.LogDebug("No source document serial updates required for pick list {AbsEntry}", absEntry);
            return;
        }

        foreach (var objectGroup in updates.GroupBy(update => update.BaseObjectType)) {
            var mapping = GetSourceDocumentMapping(objectGroup.Key);
            if (mapping == null) {
                logger.LogWarning(
                    "Skipping source document serial update for unsupported base object type {BaseObjectType} in pick list {AbsEntry}",
                    objectGroup.Key,
                    absEntry);
                continue;
            }

            foreach (var documentGroup in objectGroup.GroupBy(update => update.OrderEntry)) {
                var linePayloads = documentGroup
                .Select(update => new Dictionary<string, object?> {
                    ["LineNum"] = update.OrderRowId,
                    [mapping.SerialFieldName] = update.SerialValue
                })
                .ToArray();

                var payload = new Dictionary<string, object?> {
                    [mapping.LineCollectionName] = linePayloads
                };

                var endpoint = $"{mapping.EndpointName}({documentGroup.Key})";
                var (success, errorMessage) = await sboCompany.PatchAsync(endpoint, payload, new Dictionary<string, string> {
                    ["B1S-ReplaceCollectionsOnPatch"] = "false"
                });
                if (!success) {
                    logger.LogError(
                        "Failed to update source document serial numbers for {Endpoint} in pick list {AbsEntry}: {ErrorMessage}",
                        endpoint,
                        absEntry,
                        errorMessage);
                    throw new Exception($"Failed to update source document serial numbers for {endpoint}: {errorMessage}");
                }

                logger.LogInformation(
                    "Updated {LineCount} source document serial numbers for {Endpoint} in pick list {AbsEntry}",
                    linePayloads.Length,
                    endpoint,
                    absEntry);
            }
        }
    }

    private static SourceDocumentMapping? GetSourceDocumentMapping(int baseObjectType) => baseObjectType switch {
        17 => new SourceDocumentMapping("Orders", "DocumentLines", "SerialNum"),
        1250000001 => new SourceDocumentMapping("InventoryTransferRequests", "StockTransferLines", "SerialNumber"),
        _ => null
    };
}
