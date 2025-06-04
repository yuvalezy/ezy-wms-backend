using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.DTOs;
using Core.Enums;
using Core.Models;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class InventoryCountingCreation(SboDatabaseService dbService, SboCompany sboCompany, int countingNumber, string whsCode, int series, Dictionary<string, InventoryCountingCreationData> data)
    : IDisposable {

    private InventoryCounting? counting;
    private Recordset? rs;

    public int Entry { get; private set; }
    public int Number { get; private set; }

    public ProcessInventoryCountingResponse Execute() {
        var response = new ProcessInventoryCountingResponse();
        Company? company = null;
        
        try {
            if (sboCompany.TransactionMutex.WaitOne()) {
                try {
                    sboCompany.ConnectCompany();
                    company = sboCompany.Company!;
                    company.StartTransaction();
                    
                    CreateInventoryCounting();

                    if (company.InTransaction)
                        company.EndTransaction(BoWfTransOpt.wf_Commit);

                    response.Success = true;
                    response.ExternalEntry = Entry;
                    response.ExternalNumber = Number;
                    response.Status = ResponseStatus.Ok;
                }
                finally {
                    sboCompany.TransactionMutex.ReleaseMutex();
                }
            }
        }
        catch (Exception ex) {
            if (company?.InTransaction == true)
                company.EndTransaction(BoWfTransOpt.wf_RollBack);
            return new ProcessInventoryCountingResponse {
                Success        = false,
                ErrorMessage   = ex.Message,
                Status         = ResponseStatus.Error
            };
        }

        return response;
    }

    private void CreateInventoryCounting() {
        var company = sboCompany.Company!;
        counting = (InventoryCounting)company.GetBusinessObject(BoObjectTypes.oInventoryCountings);
        
        counting.CountDate = DateTime.Now;
        counting.Series = series;
        
        // Set reference to our counting id
        counting.Reference2 = countingNumber.ToString();

        var lines = counting.InventoryCountingLines;

        foreach (var pair in data) {
            if (!string.IsNullOrWhiteSpace(lines.ItemCode))
                lines.Add();
                
            var value = pair.Value;
            lines.ItemCode = value.ItemCode;
            lines.WarehouseCode = whsCode;
            lines.CountedQuantity = value.CountedQuantity;
            
            // SAP B1 will automatically calculate the variance based on system quantity
            
            // Add bin allocations if bins are counted
            if (value.CountedBins.Any()) {
                foreach (var bin in value.CountedBins) {
                    if (lines.BinAllocations.BinAbsEntry > 0) 
                        lines.BinAllocations.Add();
                    lines.BinAllocations.BinAbsEntry = bin.BinEntry;
                    lines.BinAllocations.Quantity = bin.CountedQuantity;
                }
            }
        }

        var result = counting.Add();
        if (result != 0) {
            var errorCode = company.GetLastErrorCode();
            var errorDescription = company.GetLastErrorDescription();
            throw new Exception($"SAP B1 Error {errorCode}: {errorDescription}");
        }

        Entry = int.Parse(company.GetNewObjectKey());
        rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
        rs.DoQuery($"select \"DocNum\" from OINC where \"DocEntry\" = {Entry}");
        Number = (int)rs.Fields.Item(0).Value;
    }

    public void Dispose() {
        if (counting != null) {
            Marshal.ReleaseComObject(counting);
            counting = null;
        }

        if (rs != null) {
            Marshal.ReleaseComObject(rs);
            rs = null;
        }

        GC.Collect();
    }
}