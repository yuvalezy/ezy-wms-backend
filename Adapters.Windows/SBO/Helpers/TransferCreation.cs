using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.DTOs;
using Core.DTOs.Transfer;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class TransferCreation(SboCompany sboCompany, int transferNumber, string whsCode, string? comments, int series, Dictionary<string, TransferCreationDataResponse> data, ILoggerFactory loggerFactory)
    : IDisposable {
    private StockTransfer? transfer;
    private Recordset?     rs;

    private readonly ILogger<TransferCreation> logger = loggerFactory.CreateLogger<TransferCreation>();

    public int Entry  { get; private set; }
    public int Number { get; private set; }

    public ProcessTransferResponse Execute() {
        var      response = new ProcessTransferResponse();
        Company? company  = null;

        logger.LogInformation("Starting transfer creation for WMS transfer {TransferNumber} in warehouse {Warehouse}", 
            transferNumber, whsCode);
        logger.LogDebug("Transfer data contains {ItemCount} items with series {Series}", data.Count, series);

        try {
            logger.LogDebug("Waiting for transaction mutex...");
            sboCompany.TransactionMutex.WaitOne();
            
            try {
                logger.LogDebug("Connecting to SAP B1 company...");
                sboCompany.ConnectCompany();
                company = sboCompany.Company!;
                
                logger.LogDebug("Starting SAP B1 transaction...");
                company.StartTransaction();

                CreateTransfer();

                if (company.InTransaction) {
                    logger.LogDebug("Committing SAP B1 transaction...");
                    company.EndTransaction(BoWfTransOpt.wf_Commit);
                }

                response.Success        = true;
                response.ExternalEntry  = Entry;
                response.ExternalNumber = Number;
                response.Status         = ResponseStatus.Ok;
                
                logger.LogInformation("Successfully created SAP B1 transfer {DocNumber} (Entry: {DocEntry}) for WMS transfer {TransferNumber}", 
                    Number, Entry, transferNumber);
            }
            finally {
                logger.LogDebug("Releasing transaction mutex");
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to create transfer for WMS transfer {TransferNumber}", transferNumber);
            
            if (company?.InTransaction == true) {
                logger.LogDebug("Rolling back SAP B1 transaction due to error");
                company.EndTransaction(BoWfTransOpt.wf_RollBack);
            }
            
            return new ProcessTransferResponse {
                Success      = false,
                ErrorMessage = ex.Message,
                Status       = ResponseStatus.Error
            };
        }

        return response;
    }

    private void CreateTransfer() {
        logger.LogDebug("Creating StockTransfer business object...");
        var company = sboCompany.Company!;
        transfer = (StockTransfer)company.GetBusinessObject(BoObjectTypes.oStockTransfer);

        transfer.DocDate = DateTime.Now;
        transfer.Series  = series;

        if (!string.IsNullOrWhiteSpace(comments)) {
            logger.LogDebug("Setting transfer comments: {Comments}", comments);
            transfer.Comments = comments;
        }

        // Set reference to our transfer id
        transfer.Reference2 = transferNumber.ToString();
        logger.LogDebug("Set transfer reference2 to WMS transfer number: {TransferNumber}", transferNumber);

        var lines = transfer.Lines;

        foreach (var pair in data) {
            logger.LogDebug("Adding line for item {ItemCode} with quantity {Quantity}", 
                pair.Value.ItemCode, pair.Value.Quantity);
                
            if (!string.IsNullOrWhiteSpace(lines.ItemCode))
                lines.Add();

            var value = pair.Value;
            lines.ItemCode          = value.ItemCode;
            lines.FromWarehouseCode = whsCode;
            lines.WarehouseCode     = whsCode;
            lines.Quantity          = (double)value.Quantity;
            lines.UseBaseUnits      = BoYesNoEnum.tYES;

            // Add source bin allocations
            if (value.SourceBins.Any()) {
                logger.LogDebug("Adding {Count} source bin allocations for item {ItemCode}", 
                    value.SourceBins.Count, value.ItemCode);
            }
            
            foreach (var source in value.SourceBins) {
                if (lines.BinAllocations.BinAbsEntry > 0)
                    lines.BinAllocations.Add();
                lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                lines.BinAllocations.BinAbsEntry   = source.BinEntry;
                lines.BinAllocations.Quantity      = (double)source.Quantity;
                
                logger.LogDebug("Added source bin {BinEntry} with quantity {Quantity}", 
                    source.BinEntry, source.Quantity);
            }

            // Add target bin allocations
            if (value.TargetBins.Any()) {
                logger.LogDebug("Adding {Count} target bin allocations for item {ItemCode}", 
                    value.TargetBins.Count, value.ItemCode);
            }
            
            foreach (var target in value.TargetBins) {
                if (lines.BinAllocations.BinAbsEntry > 0)
                    lines.BinAllocations.Add();
                lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                lines.BinAllocations.BinAbsEntry   = target.BinEntry;
                lines.BinAllocations.Quantity      = (double)target.Quantity;
                
                logger.LogDebug("Added target bin {BinEntry} with quantity {Quantity}", 
                    target.BinEntry, target.Quantity);
            }
        }

        logger.LogInformation("Calling SAP B1 transfer.Add() with {LineCount} lines...", data.Count);
        var result = transfer.Add();
        
        if (result != 0) {
            var errorCode        = company.GetLastErrorCode();
            var errorDescription = company.GetLastErrorDescription();
            logger.LogError("SAP B1 transfer creation failed with error code {ErrorCode}: {ErrorDescription}", 
                errorCode, errorDescription);
            throw new Exception($"SAP B1 Error {errorCode}: {errorDescription}");
        }

        Entry = int.Parse(company.GetNewObjectKey());
        logger.LogDebug("Transfer created successfully with DocEntry: {DocEntry}", Entry);
        
        rs    = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
        rs.DoQuery($"select \"DocNum\" from OWTR where \"DocEntry\" = {Entry}");
        Number = (int)rs.Fields.Item(0).Value;
        
        logger.LogDebug("Retrieved DocNum: {DocNum} for DocEntry: {DocEntry}", Number, Entry);
    }

    public void Dispose() {
        logger.LogDebug("Disposing TransferCreation resources...");
        
        if (transfer != null) {
            logger.LogDebug("Releasing StockTransfer COM object");
            Marshal.ReleaseComObject(transfer);
            transfer = null;
        }

        if (rs != null) {
            logger.LogDebug("Releasing Recordset COM object");
            Marshal.ReleaseComObject(rs);
            rs = null;
        }

        logger.LogDebug("Forcing garbage collection for COM cleanup");
        GC.Collect();
    }
}