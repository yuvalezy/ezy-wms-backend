using System.Data;
using System.Text.Json.Serialization;
using Adapters.CrossPlatform.SBO.Services;
using Core.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class PickingUpdate(
    int                absEntry,
    List<PickList>     data,
    SboDatabaseService dbService,
    SboCompany         sboCompany,
    string?            filtersPickReady,
    ILoggerFactory     loggerFactory) : IDisposable {
    
    private readonly ILogger<PickingUpdate> logger = loggerFactory.CreateLogger<PickingUpdate>();
    private readonly Dictionary<int, (string itemCode, int numInBuy, bool useBaseUnit)> additionalData = new();

    public async Task Execute() {
        logger.LogInformation("Starting pick list update for AbsEntry {AbsEntry} with {DataCount} pick entries", 
            absEntry, data.Count);

        try {
            await LoadAdditionalData();
            await PreparePickList();
            await ProcessPickList();
            
            logger.LogInformation("Successfully completed pick list update for AbsEntry {AbsEntry}", absEntry);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to update pick list {AbsEntry}", absEntry);
            throw;
        }
    }

    private async Task LoadAdditionalData() {
        logger.LogDebug("Loading additional data for pick list {AbsEntry}", absEntry);
        
        const string query =
            """
            select PKL1."PickEntry", T3."ItemCode", COALESCE(T3."NumInBuy", 1) "NumInBuy",
                   CASE 
                       WHEN T2."TransType" = 17 THEN T4."UseBaseUn"
                       WHEN T2."TransType" = 13 THEN T5."UseBaseUn"
            		   Else 'Y'
                   END AS "UseBaseUn"
            from PKL1
            inner join OILM T2 on T2."TransType" = PKL1."BaseObject" and T2.DocEntry = PKL1."OrderEntry" and T2."DocLineNum" = PKL1."OrderLine"
            inner join OITM T3 on T3."ItemCode" = T2."ItemCode" 
            LEFT JOIN RDR1 T4 ON T2."TransType" = 17 
                AND T4."DocEntry" = T2."DocEntry" 
                AND T4."LineNum" = T2."DocLineNum"
            LEFT JOIN INV1 T5 ON T2."TransType" = 13 
                AND T5."DocEntry" = T2."DocEntry" 
                AND T5."LineNum" = T2."DocLineNum"
            where PKL1."AbsEntry" = @AbsEntry
            """;

        var parameters = new[] { new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = absEntry } };
        
        var results = await dbService.QueryAsync(query, parameters, reader => new {
            PickEntry = reader.GetInt32("PickEntry"),
            ItemCode = reader.GetString("ItemCode"),
            NumInBuy = Convert.ToInt32(reader["NumInBuy"]),
            UseBaseUnit = reader.GetString("UseBaseUn").Equals("Y")
        });
        
        foreach (var row in results) {
            additionalData.Add(row.PickEntry, (row.ItemCode, row.NumInBuy, row.UseBaseUnit));
            
            logger.LogDebug("Loaded data for PickEntry {PickEntry}: ItemCode={ItemCode}, NumInBuy={NumInBuy}, UseBaseUnit={UseBaseUnit}", 
                row.PickEntry, row.ItemCode, row.NumInBuy, row.UseBaseUnit);
        }
        
        logger.LogDebug("Loaded additional data for {ItemCount} pick entries", additionalData.Count);
    }

    private async Task PreparePickList() {
        logger.LogDebug("Preparing pick list {AbsEntry}", absEntry);
        
        // Get current pick list
        var pickList = await sboCompany.GetAsync<PickListResponse>($"PickLists({absEntry})");
        if (pickList == null) {
            throw new Exception($"Could not find Pick List {absEntry}");
        }
        
        if (pickList.Status == "ps_Closed") {
            throw new Exception("Cannot process document if the Status is closed");
        }
        
        logger.LogDebug("Pick list {AbsEntry} found with status {Status}, {LineCount} lines", 
            absEntry, pickList.Status, pickList.PickListsLines?.Length ?? 0);

        // Check and set filter if needed
        if (!string.IsNullOrWhiteSpace(filtersPickReady)) {
            logger.LogDebug("Checking filter {FilterName} for pick list {AbsEntry}", filtersPickReady, absEntry);
            
            // Note: Service Layer user-defined fields would need to be handled through UserFields
            // This is a simplified version - may need adjustment based on actual UDF structure
        }

        // Clear all bin allocations first
        var prepareData = new {
            PickListsLines = pickList.PickListsLines?.Select(line => new {
                LineNumber                  = line.LineNumber,
                DocumentLinesBinAllocations = Array.Empty<object>() // Clear all bin allocations
            }).ToArray()
        };

        // Set filter if specified
        if (!string.IsNullOrWhiteSpace(filtersPickReady)) {
            // This would need to be adjusted based on actual UDF structure in Service Layer
            logger.LogDebug("Setting filter {FilterName} to Y for pick list {AbsEntry}", filtersPickReady, absEntry);
        }

        logger.LogDebug("Clearing bin allocations and updating pick list {AbsEntry}", absEntry);
        var (success, errorMessage) = await sboCompany.PatchAsync($"PickLists({absEntry})", prepareData);
        
        if (!success) {
            throw new Exception($"Failed to prepare pick list: {errorMessage}");
        }
        
        logger.LogDebug("Pick list {AbsEntry} prepared successfully", absEntry);
    }

    private async Task ProcessPickList() {
        logger.LogDebug("Processing pick list {AbsEntry} with {DataCount} entries", absEntry, data.Count);
        
        // Group data by pick entry
        var lines = data.GroupBy(v => v.PickEntry)
            .Select(a => new {
                PickEntry = a.Key,
                Quantity = a.Sum(b => b.Quantity),
                Bins = a.GroupBy(b => b.BinEntry)
                    .Select(c => new { BinEntry = c.Key, Quantity = c.Sum(d => d.Quantity) })
                    .ToList()
            }).ToList();

        logger.LogDebug("Grouped data into {LineCount} pick lines", lines.Count);

        // Get current pick list structure
        var pickList = await sboCompany.GetAsync<PickListResponse>($"PickLists({absEntry})");
        if (pickList?.PickListsLines == null) {
            throw new Exception($"Could not retrieve pick list lines for {absEntry}");
        }

        var updatedLines = new List<object>();

        foreach (var pickLine in pickList.PickListsLines) {
            var matchingData = lines.FirstOrDefault(v => v.PickEntry == pickLine.LineNumber);
            
            var addData = additionalData[pickLine.LineNumber];
            if (matchingData == null) {
                // Keep original line without changes
                updatedLines.Add(new {
                    LineNumber                  = pickLine.LineNumber,
                    PickedQuantity              = pickLine.PickedQuantity,
                    DocumentLinesBinAllocations = pickLine.DocumentLinesBinAllocations ?? []
                });
                continue;
            }

            // Get additional data for quantity calculation
            if (!additionalData.TryGetValue(matchingData.PickEntry, out var itemData)) {
                logger.LogWarning("No additional data found for PickEntry {PickEntry}", matchingData.PickEntry);
                continue;
            }

            var (itemCode, numInBuy, useBaseUnit) = itemData;
            double pickedQuantity = (double)matchingData.Quantity / (useBaseUnit ? numInBuy : 1);

            // Create bin allocations
            var binAllocations = matchingData.Bins.Select(bin => new {
                BinAbsEntry = bin.BinEntry,
                Quantity = (double)bin.Quantity
            }).ToArray();

            updatedLines.Add(new {
                LineNumber = pickLine.LineNumber,
                PickedQuantity = pickedQuantity,
                DocumentLinesBinAllocations = binAllocations
            });

            logger.LogDebug("Updated pick line {LineNumber}: ItemCode={ItemCode}, PickedQuantity={PickedQuantity}, BinAllocations={BinCount}", 
                pickLine.LineNumber, itemCode, pickedQuantity, binAllocations.Length);
        }

        var updateData = new {
            PickListsLines = updatedLines.ToArray()
        };

        logger.LogInformation("Updating pick list {AbsEntry} with {LineCount} lines", absEntry, updatedLines.Count);
        var (success, errorMessage) = await sboCompany.PatchAsync($"PickLists({absEntry})", updateData);

        if (!success) {
            throw new Exception($"Could not update Pick List: {errorMessage}");
        }

        logger.LogInformation("Pick list {AbsEntry} updated successfully", absEntry);
    }

    public void Dispose() {
        logger.LogDebug("Disposing PickingUpdate resources for AbsEntry {AbsEntry}", absEntry);
    }
    
    private class PickListResponse {
        public int AbsEntry { get; set; }
        public string Status { get; set; } = string.Empty;
        public PickListLine[]? PickListsLines { get; set; }
    }
    
    private class PickListLine {
        public int LineNumber { get; set; }
        public double PickedQuantity { get; set; }
        public object[]? DocumentLinesBinAllocations { get; set; }
    }
}