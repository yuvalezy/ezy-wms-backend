using System.Data;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Adapters.Common.SBO.Services;
using Adapters.CrossPlatform.SBO.Services;

namespace Customer.Extensions;

public class PackagingCalculatorPostProcessor : IPickingPostProcessor {
    public string Id => "customer-packaging-calculator";

    public async Task ExecuteAsync(PickingPostProcessorContext context, CancellationToken cancellationToken = default) {
        var logger = context.Logger;
        var serviceProvider = context.ServiceProvider;
        
        logger.LogInformation("Starting packaging calculation for pick list {AbsEntry}", context.AbsEntry);

        try {
            // Get required services from DI
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
            var sboDatabase = scope.ServiceProvider.GetRequiredService<SboDatabaseService>();
            var sboCompany = scope.ServiceProvider.GetRequiredService<SboCompany>();

            // Step 1: Query EF database for picking data with package barcodes
            var pickingData = await GetPickingDataWithBarcodes(context.AbsEntry, dbContext, logger);
            
            if (!pickingData.Any()) {
                logger.LogWarning("No picking data with packages found for pick list {AbsEntry}", context.AbsEntry);
                return;
            }

            // Step 2: Execute complex CTE query against SAP database
            var packageCalculations = await ExecutePackagingCalculation(pickingData, context.AbsEntry, sboDatabase, logger);
            
            if (!packageCalculations.Any()) {
                logger.LogWarning("No package calculations generated for pick list {AbsEntry}", context.AbsEntry);
                return;
            }

            // Step 3: Update SAP Orders via Service Layer
            await UpdateOrdersWithPackaging(packageCalculations, sboCompany, logger);
            
            logger.LogInformation("Successfully completed packaging calculation for pick list {AbsEntry}", context.AbsEntry);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to execute packaging calculation for pick list {AbsEntry}", context.AbsEntry);
            throw;
        }
    }

    public bool IsEnabled(Dictionary<string, object>? configuration) {
        // Enable this processor if configuration is provided
        return configuration?.ContainsKey("Enabled") == true && 
               (bool)(configuration["Enabled"] ?? false);
    }

    private async Task<List<PickingDataWithBarcode>> GetPickingDataWithBarcodes(int absEntry, SystemDbContext dbContext, ILogger logger) {
        logger.LogDebug("Querying EF database for picking data with barcodes for AbsEntry {AbsEntry}", absEntry);

        var query = @"
            select T0.""PickEntry"", 
                   ROW_NUMBER() OVER (PARTITION BY T0.""PickEntry"", T0.""ItemCode"", T0.""Unit"" ORDER BY T0.""PickEntry"") RowNumber, 
                   T0.""ItemCode"", 
                   T0.""Quantity"", 
                   T2.""Barcode""
            from ""PickLists"" T0
                     left outer join ""PackageCommitments"" T1 on T1.""SourceOperationId"" = T0.""Id""
                     left outer join ""Packages"" T2 on T2.""Id"" = T1.""PackageId""
            where T0.""AbsEntry"" = {0}
            order by T0.""PickEntry""";

        var result = await dbContext.Database
            .SqlQueryRaw<PickingDataWithBarcode>(query, absEntry)
            .ToListAsync();

        logger.LogDebug("Found {Count} picking records with barcode data", result.Count);
        return result;
    }

    private async Task<List<PackageCalculationResult>> ExecutePackagingCalculation(
        List<PickingDataWithBarcode> pickingData, 
        int absEntry, 
        SboDatabaseService sboDatabase, 
        ILogger logger) {
        
        logger.LogDebug("Executing packaging calculation against SAP database for {Count} records", pickingData.Count);

        // Build the VALUES clause for the CTE from our picking data
        var valuesClauses = pickingData.Select(p => 
            $"({p.PickEntry}, {p.RowNumber}, N'{p.ItemCode}', {p.Quantity}, {(p.Barcode != null ? $"N'{p.Barcode}'" : "null")})");

        var valuesClause = string.Join(",\n              ", valuesClauses);

        var query = $@"
WITH src AS (
    SELECT *
    FROM (VALUES
              {valuesClause}
         ) AS t(""PickEntry"", ""RowNum"", ""ItemCode"", ""Quantity"", ""Barcode"")
),
     ordered AS (
         SELECT s.*,
                ROW_NUMBER() OVER (ORDER BY s.""PickEntry"", s.""RowNum"") AS seq
         FROM src s
     ),
     with_first AS (
         SELECT o.*,
                MIN(o.seq) OVER (PARTITION BY o.""Barcode"") AS first_seq
         FROM ordered o
     ),
     with_packnum AS (
         SELECT w.*,
                CASE WHEN w.""Barcode"" IS NULL THEN NULL
                     ELSE DENSE_RANK() OVER (ORDER BY w.first_seq) END AS ""PackNumber""
         FROM with_first w
     ),
     with_packrow AS (
         SELECT x.*,
                ROW_NUMBER() OVER (PARTITION BY x.""PackNumber"" ORDER BY x.seq) AS pack_rownum
         FROM with_packnum x
     ),
-- Non-packaged items (barcode is null)
     non_packaged AS (
         SELECT p.""BaseObject"",
                p.""OrderEntry"",
                p.""OrderLine"",
                CEILING(w.""Quantity"" / i.""PurPackUn"" / i.""NumInBuy"") AS ""Packs"",
                NULL AS ""Barcode""
         FROM with_packrow w
                  JOIN OITM i ON i.ItemCode = w.""ItemCode""
                  JOIN PKL1 p ON p.""AbsEntry"" = {absEntry} AND p.""PickEntry"" = w.""PickEntry""
         WHERE w.""Barcode"" IS NULL
     ),
-- Packaged items (barcode not null)
     packaged AS (
         SELECT p.""BaseObject"",
                p.""OrderEntry"",
                p.""OrderLine"",
                CASE WHEN w.pack_rownum = 1 THEN 1 ELSE 0 END AS ""Packs"",
                'R' + CAST(DENSE_RANK() OVER (ORDER BY w.first_seq) AS VARCHAR) AS ""Barcode""
         FROM with_packrow w
                  JOIN OITM i ON i.ItemCode = w.""ItemCode""
                  JOIN PKL1 p ON p.""AbsEntry"" = {absEntry} AND p.""PickEntry"" = w.""PickEntry""
         WHERE w.""Barcode"" IS NOT NULL
     )
-- Final union
SELECT * FROM non_packaged
UNION ALL
SELECT * FROM packaged
ORDER BY ""OrderLine"", ""Packs"" DESC";

        var results = await sboDatabase.QueryAsync(query, null, reader => new PackageCalculationResult {
            BaseObject = Convert.ToInt32(reader["BaseObject"]),
            OrderEntry = Convert.ToInt32(reader["OrderEntry"]), 
            OrderLine = Convert.ToInt32(reader["OrderLine"]),
            Packs = Convert.ToInt32(reader["Packs"]),
            Barcode = reader["Barcode"]?.ToString()
        });

        var resultList = results.ToList();
        logger.LogDebug("Generated {Count} package calculation results", resultList.Count);
        
        return resultList;
    }

    private async Task UpdateOrdersWithPackaging(
        List<PackageCalculationResult> packageCalculations, 
        SboCompany sboCompany, 
        ILogger logger) {
        
        logger.LogDebug("Updating {Count} order lines with packaging data", packageCalculations.Count);

        // Group by OrderEntry to batch updates per order
        var orderGroups = packageCalculations.GroupBy(p => p.OrderEntry);

        foreach (var orderGroup in orderGroups) {
            try {
                var orderEntry = orderGroup.Key;
                logger.LogDebug("Updating order {OrderEntry} with {LineCount} line updates", orderEntry, orderGroup.Count());

                foreach (var calc in orderGroup) {
                    try {
                        // Update the specific order line
                        var updateData = new {
                            SerialNum = calc.Barcode,
                            PackQty = calc.Packs
                        };

                        var result = await sboCompany.PatchAsync($"Orders({calc.OrderEntry})/Lines({calc.OrderLine})", updateData);
                        
                        if (!result.success) {
                            logger.LogError("Failed to update Order {OrderEntry} Line {OrderLine}: {Error}", 
                                calc.OrderEntry, calc.OrderLine, result.errorMessage);
                        } else {
                            logger.LogDebug("Successfully updated Order {OrderEntry} Line {OrderLine} with SerialNum={SerialNum}, PackQty={PackQty}", 
                                calc.OrderEntry, calc.OrderLine, calc.Barcode, calc.Packs);
                        }
                    }
                    catch (Exception ex) {
                        logger.LogError(ex, "Error updating Order {OrderEntry} Line {OrderLine}", calc.OrderEntry, calc.OrderLine);
                    }
                }
            }
            catch (Exception ex) {
                logger.LogError(ex, "Error processing order group {OrderEntry}", orderGroup.Key);
            }
        }

        logger.LogInformation("Completed order updates for packaging data");
    }
}

// Data models for the post processor
public class PickingDataWithBarcode {
    public int PickEntry { get; set; }
    public int RowNumber { get; set; }
    public required string ItemCode { get; set; }
    public int Quantity { get; set; }
    public string? Barcode { get; set; }
}

public class PackageCalculationResult {
    public int BaseObject { get; set; }
    public int OrderEntry { get; set; }
    public int OrderLine { get; set; }
    public int Packs { get; set; }
    public string? Barcode { get; set; }
}