using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.DTOs;
using Core.Enums;
using Core.Models;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class TransferCreation(SboDatabaseService dbService, SboCompany sboCompany, Guid transferId, string whsCode, string? comments, Dictionary<string, TransferCreationData> data)
    : IDisposable {

    private StockTransfer? transfer;
    private Recordset? rs;

    public int Entry { get; private set; }
    public int Number { get; private set; }

    public ProcessTransferResponse Execute() {
        var response = new ProcessTransferResponse();
        Company? company = null;
        
        try {
            if (sboCompany.TransactionMutex.WaitOne()) {
                try {
                    sboCompany.ConnectCompany();
                    company = sboCompany.Company!;
                    company.StartTransaction();

                    // Get transfer series (using default series for now)
                    int transferSeries = GetDefaultSeries();
                    
                    CreateTransfer(transferSeries);

                    if (company.InTransaction)
                        company.EndTransaction(BoWfTransOpt.wf_Commit);

                    response.Success = true;
                    response.SapDocEntry = Entry;
                    response.SapDocNumber = Number;
                    response.Status = ResponseStatus.Ok;
                }
                finally {
                    sboCompany.TransactionMutex.ReleaseMutex();
                }
            }
        }
        catch (Exception ex) {
            company?.EndTransaction(BoWfTransOpt.wf_RollBack);
            response.Success = false;
            response.ErrorMessage = $"Error generating Stock Transfer: {ex.Message}";
            response.Status = ResponseStatus.Error;
        }

        return response;
    }

    private void CreateTransfer(int transferSeries) {
        var company = sboCompany.Company!;
        transfer = (StockTransfer)company.GetBusinessObject(BoObjectTypes.oStockTransfer);
        
        transfer.DocDate = DateTime.Now;
        transfer.Series = transferSeries;
        
        if (!string.IsNullOrWhiteSpace(comments))
            transfer.Comments = comments;

        // Set custom field to link back to our transfer
        transfer.UserFields.Fields.Item("U_LW_TRANSFER").Value = transferId.ToString();

        var lines = transfer.Lines;

        foreach (var pair in data) {
            if (!string.IsNullOrWhiteSpace(lines.ItemCode))
                lines.Add();
                
            var value = pair.Value;
            lines.ItemCode = value.ItemCode;
            lines.FromWarehouseCode = whsCode;
            lines.WarehouseCode = whsCode;
            lines.Quantity = (double)value.Quantity;
            lines.UseBaseUnits = BoYesNoEnum.tYES;

            // Add source bin allocations
            foreach (var source in value.SourceBins) {
                if (lines.BinAllocations.BinAbsEntry > 0) 
                    lines.BinAllocations.Add();
                lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                lines.BinAllocations.BinAbsEntry = source.BinEntry;
                lines.BinAllocations.Quantity = (double)source.Quantity;
            }
            
            // Add target bin allocations
            foreach (var target in value.TargetBins) {
                if (lines.BinAllocations.BinAbsEntry > 0) 
                    lines.BinAllocations.Add();
                lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                lines.BinAllocations.BinAbsEntry = target.BinEntry;
                lines.BinAllocations.Quantity = (double)target.Quantity;
            }
        }

        if (transfer.Add() != 0) {
            throw new Exception(company.GetLastErrorDescription());
        }

        Entry = int.Parse(company.GetNewObjectKey());
        rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
        rs.DoQuery($"select \"DocNum\" from OWTR where \"DocEntry\" = {Entry}");
        Number = (int)rs.Fields.Item(0).Value;
    }

    private int GetDefaultSeries() {
        // For now, return -1 to use default series
        // In a full implementation, you would query the series from SAP B1
        return -1;
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