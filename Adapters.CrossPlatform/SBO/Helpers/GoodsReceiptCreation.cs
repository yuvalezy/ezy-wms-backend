using System.Text.Json.Serialization;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.GoodsReceipt;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class GoodsReceiptCreation(
    SboCompany                                                 sboCompany,
    int                                                        number,
    string                                                     whsCode,
    int                                                        series,
    Dictionary<string, List<GoodsReceiptCreationDataResponse>> data,
    ILoggerFactory                                             loggerFactory) : IDisposable {
    private readonly ILogger<GoodsReceiptCreation> logger = loggerFactory.CreateLogger<GoodsReceiptCreation>();

    private List<(int Entry, int Number)> NewEntries { get; } = [];

    public async Task<ProcessGoodsReceiptResult> Execute() {
        logger.LogInformation("Starting goods receipt creation for WMS receipt {Number} in warehouse {Warehouse}", number, whsCode);

        try {
            // Group data by source documents for batch creation
            var groupedData = GroupDataBySource();

            foreach (var group in groupedData) {
                var result = await CreateDocument(group.Key, group.Value);
                if (!result.success) {
                    return new ProcessGoodsReceiptResult {
                        Success      = false,
                        ErrorMessage = result.errorMessage ?? "Failed to create goods receipt document"
                    };
                }
            }

            logger.LogInformation("Successfully created {DocumentCount} goods receipt documents for WMS receipt {Number}",
                NewEntries.Count, number);

            return new ProcessGoodsReceiptResult {
                Success        = true,
                DocumentNumber = NewEntries.FirstOrDefault().Number
            };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to create goods receipt for WMS receipt {Number}", number);

            return new ProcessGoodsReceiptResult {
                Success      = false,
                ErrorMessage = $"Error generating Goods Receipt: {ex.Message}"
            };
        }
    }

    private Dictionary<(string? CardCode, int Type, int Entry), List<GoodsReceiptCreationDataResponse>> GroupDataBySource() {

        var grouped = new Dictionary<(string? CardCode, int Type, int Entry), List<GoodsReceiptCreationDataResponse>>();

        foreach (var item in data.SelectMany(kvp => kvp.Value)) {
            if (item.Sources.Any()) {
                foreach (var source in item.Sources) {
                    var key = (CardCode: (string?)null, source.SourceType, source.SourceEntry);
                    if (!grouped.ContainsKey(key))
                        grouped[key] = new List<GoodsReceiptCreationDataResponse>();
                    grouped[key].Add(item);

                    logger.LogDebug("Added item {ItemCode} to group with source type {SourceType}, entry {SourceEntry}",
                        item.ItemCode, source.SourceType, source.SourceEntry);
                }
            }
            else {
                // Items without sources go into a general receipt
                var key = (CardCode: (string?)null, Type: 0, Entry: 0);
                if (!grouped.ContainsKey(key))
                    grouped[key] = new List<GoodsReceiptCreationDataResponse>();
                grouped[key].Add(item);
            }
        }

        return grouped;
    }

    private async Task<(bool success, string? errorMessage)> CreateDocument(
        (string? CardCode, int Type, int Entry) key,
        List<GoodsReceiptCreationDataResponse>  items) {
        logger.LogDebug("Creating goods receipt document for group: CardCode={CardCode}, Type={Type}, Entry={Entry} with {ItemCount} items",
            key.CardCode, key.Type, key.Entry, items.Count);

        try {
            var lines = new List<DocumentLine>();

            foreach (var item in items) {
                DocumentLine line;

                // Add source document linking if applicable
                if (item.Sources.Any()) {
                    var source = item.Sources.First();
                    line = new DocumentLine {
                        ItemCode      = item.ItemCode,
                        Quantity      = item.Quantity,
                        WarehouseCode = whsCode,
                        FreeText      = item.Comments ?? "",
                        BaseType      = source.SourceType,
                        BaseEntry     = source.SourceEntry,
                        BaseLine      = source.SourceLine
                    };

                    logger.LogDebug("Added line for item {ItemCode} with source link: Type={BaseType}, Entry={BaseEntry}, Line={BaseLine}",
                        item.ItemCode, source.SourceType, source.SourceEntry, source.SourceLine);
                }
                else {
                    line = new DocumentLine {
                        ItemCode      = item.ItemCode,
                        Quantity      = item.Quantity,
                        WarehouseCode = whsCode,
                        FreeText      = item.Comments ?? ""
                    };
                }

                lines.Add(line);
            }

            var documentData = new {
                DocDate       = DateTime.Now.ToString("yyyy-MM-dd"),
                TaxDate       = DateTime.Now.ToString("yyyy-MM-dd"),
                Series        = series,
                CardCode      = key.CardCode,
                Reference2    = number.ToString(),
                Comments      = $"Generated from WMS Goods Receipt #{number}",
                DocumentLines = lines
            };

            logger.LogInformation("Calling Service Layer PurchaseDeliveryNotes POST with {LineCount} lines...", lines.Count);
            var (success, errorMessage, result) = await sboCompany.PostAsync<PurchaseDeliveryNoteResponse>("PurchaseDeliveryNotes", documentData);

            if (success && result != null) {
                NewEntries.Add((result.DocEntry, result.DocNum));

                logger.LogInformation("Successfully created goods receipt document {DocNum} (Entry: {DocEntry})",
                    result.DocNum, result.DocEntry);

                return (true, null);
            }
            else {
                logger.LogError("Failed to create goods receipt document: {ErrorMessage}", errorMessage);
                return (false, errorMessage ?? "Failed to create document");
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Exception while creating goods receipt document");
            return (false, ex.Message);
        }
    }

    public void Dispose() {
    }

    private class DocumentLine {
        public string  ItemCode      { get; set; } = string.Empty;
        public decimal Quantity      { get; set; }
        public string  WarehouseCode { get; set; } = string.Empty;
        public string  FreeText      { get; set; } = string.Empty;
        public int?    BaseType      { get; set; }
        public int?    BaseEntry     { get; set; }
        public int?    BaseLine      { get; set; }
    }

    private class PurchaseDeliveryNoteResponse {
        public int DocEntry { get; set; }
        public int DocNum   { get; set; }
    }
}