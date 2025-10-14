using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.DTOs.Transfer;
using Core.Enums;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class TransferCreation(
    SboCompany sboCompany,
    int transferNumber,
    string sourceWarehouse,
    string? targetWarehouse,
    string? comments,
    int series,
    Dictionary<string, TransferCreationDataResponse> data,
    ILoggerFactory loggerFactory)
    : IDisposable {
    private StockTransfer? transfer;
    private Recordset?     rs;

    private readonly ILogger<TransferCreation> logger = loggerFactory.CreateLogger<TransferCreation>();

    public int Entry  { get; private set; }
    public int Number { get; private set; }

    public ProcessTransferResponse Execute() {
        var      response = new ProcessTransferResponse();
        Company? company  = null;

        logger.LogInformation("Starting transfer creation for WMS transfer {TransferNumber} from warehouse {Warehouse} to warehouse {TargetWarehouse}", transferNumber, sourceWarehouse, targetWarehouse);
        try {
            sboCompany.TransactionMutex.WaitOne();
            
            try {
                sboCompany.ConnectCompany();
                company = sboCompany.Company!;
                company.StartTransaction();

                CreateTransfer();

                if (company.InTransaction) {
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
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to create transfer for WMS transfer {TransferNumber}", transferNumber);
            
            if (company?.InTransaction == true) {
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
        var company = sboCompany.Company!;
        transfer = (StockTransfer)company.GetBusinessObject(BoObjectTypes.oStockTransfer);

        transfer.DocDate = DateTime.Now;
        transfer.Series  = series;

        if (!string.IsNullOrWhiteSpace(comments)) {
            transfer.Comments = comments;
        }

        // Set reference to our transfer id
        transfer.Reference2 = transferNumber.ToString();

        var lines = transfer.Lines;

        foreach (var pair in data) {
            logger.LogDebug("Adding line for item {ItemCode} with quantity {Quantity}", 
                pair.Value.ItemCode, pair.Value.Quantity);
                
            if (!string.IsNullOrWhiteSpace(lines.ItemCode))
                lines.Add();

            var value = pair.Value;
            lines.ItemCode          = value.ItemCode;
            lines.FromWarehouseCode = sourceWarehouse;
            lines.WarehouseCode     = targetWarehouse ?? sourceWarehouse;
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
        
        rs    = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
        rs.DoQuery($"select \"DocNum\" from OWTR where \"DocEntry\" = {Entry}");
        Number = (int)rs.Fields.Item(0).Value;
    }

    public void Dispose() {
        
        if (transfer != null) {
            Marshal.ReleaseComObject(transfer);
            transfer = null;
        }

        if (rs != null) {
            Marshal.ReleaseComObject(rs);
            rs = null;
        }
        GC.Collect();
    }
}