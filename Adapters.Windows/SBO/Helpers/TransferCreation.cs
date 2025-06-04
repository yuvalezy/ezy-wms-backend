using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.DTOs;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class TransferCreation(SboCompany sboCompany, int transferNumber, string whsCode, string? comments, int series, Dictionary<string, TransferCreationData> data, ILoggerFactory loggerFactory)
    : IDisposable {
    private StockTransfer? transfer;
    private Recordset?     rs;

    private readonly ILogger<CountingCreation> logger = new Logger<CountingCreation>(loggerFactory);

    public int Entry  { get; private set; }
    public int Number { get; private set; }

    public ProcessTransferResponse Execute() {
        var      response = new ProcessTransferResponse();
        Company? company  = null;

        try {
            sboCompany.TransactionMutex.WaitOne();
            try {
                sboCompany.ConnectCompany();
                company = sboCompany.Company!;
                company.StartTransaction();

                CreateTransfer();

                if (company.InTransaction)
                    company.EndTransaction(BoWfTransOpt.wf_Commit);

                response.Success        = true;
                response.ExternalEntry  = Entry;
                response.ExternalNumber = Number;
                response.Status         = ResponseStatus.Ok;
            }
            finally {
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception ex) {
            if (company?.InTransaction == true)
                company.EndTransaction(BoWfTransOpt.wf_RollBack);
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

        if (!string.IsNullOrWhiteSpace(comments))
            transfer.Comments = comments;

        // Set reference to our transfer id
        transfer.Reference2 = transferNumber.ToString();

        var lines = transfer.Lines;

        foreach (var pair in data) {
            if (!string.IsNullOrWhiteSpace(lines.ItemCode))
                lines.Add();

            var value = pair.Value;
            lines.ItemCode          = value.ItemCode;
            lines.FromWarehouseCode = whsCode;
            lines.WarehouseCode     = whsCode;
            lines.Quantity          = (double)value.Quantity;
            lines.UseBaseUnits      = BoYesNoEnum.tYES;

            // Add source bin allocations
            foreach (var source in value.SourceBins) {
                if (lines.BinAllocations.BinAbsEntry > 0)
                    lines.BinAllocations.Add();
                lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                lines.BinAllocations.BinAbsEntry   = source.BinEntry;
                lines.BinAllocations.Quantity      = (double)source.Quantity;
            }

            // Add target bin allocations
            foreach (var target in value.TargetBins) {
                if (lines.BinAllocations.BinAbsEntry > 0)
                    lines.BinAllocations.Add();
                lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                lines.BinAllocations.BinAbsEntry   = target.BinEntry;
                lines.BinAllocations.Quantity      = (double)target.Quantity;
            }
        }

        var result = transfer.Add();
        if (result != 0) {
            var errorCode        = company.GetLastErrorCode();
            var errorDescription = company.GetLastErrorDescription();
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