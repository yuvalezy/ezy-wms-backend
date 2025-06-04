using System.Data;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.DTOs;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class CountingCreation(
    SboCompany                                        sboCompany,
    int                                               countingNumber,
    string                                            whsCode,
    int                                               series,
    Dictionary<string, InventoryCountingCreationData> data,
    ILoggerFactory                                    loggerFactory)
    : IDisposable {
    private          CompanyService?            companyService;
    private          InventoryCountingsService? service;
    private          InventoryCounting?         counting;
    private          Recordset?                 rs;
    private readonly ILogger<CountingCreation>  logger = new Logger<CountingCreation>(loggerFactory);

    public (int Entry, int Number) NewEntry { get; private set; }

    public ProcessInventoryCountingResponse Execute() {
        var response = new ProcessInventoryCountingResponse();
        try {
            sboCompany.TransactionMutex.WaitOne();
            try {
                sboCompany.ConnectCompany();
                sboCompany.Company!.StartTransaction();
                CreateCounting();
                if (sboCompany.Company.InTransaction)
                    sboCompany.Company.EndTransaction(BoWfTransOpt.wf_Commit);
                response.Success        = true;
                response.ExternalEntry  = NewEntry.Entry;
                response.ExternalNumber = NewEntry.Number;
                response.Status         = ResponseStatus.Ok;
            }
            finally {
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception e) {
            if (sboCompany.Company?.InTransaction == true)
                sboCompany.Company.EndTransaction(BoWfTransOpt.wf_RollBack);
            response.ErrorMessage = e.Message;
            response.Status       = ResponseStatus.Error;
        }

        return response;
    }

    private void CreateCounting() {
        companyService      = sboCompany.Company!.GetCompanyService();
        service             = (InventoryCountingsService)companyService.GetBusinessService(ServiceTypes.InventoryCountingsService);
        counting            = (InventoryCounting)service.GetDataInterface(InventoryCountingsServiceDataInterfaces.icsInventoryCounting);
        counting.Series     = series;
        counting.Reference2 = countingNumber.ToString();
        foreach (var value in data) {
            if (value.Value.CountedBins.Count > 0) {
                foreach (var countedBin in value.Value.CountedBins) {
                    var line = counting.InventoryCountingLines.Add();
                    line.ItemCode        = value.Value.ItemCode;
                    line.WarehouseCode   = whsCode;
                    line.BinEntry        = countedBin.BinEntry;
                    line.Counted         = BoYesNoEnum.tYES;
                    line.CountedQuantity = countedBin.CountedQuantity;
                }
            }
            else {
                var line = counting.InventoryCountingLines.Add();
                line.ItemCode        = value.Value.ItemCode;
                line.WarehouseCode   = whsCode;
                line.Counted         = BoYesNoEnum.tYES;
                line.CountedQuantity = value.Value.CountedQuantity;
            }
        }

        var @params = service.Add(counting);
        NewEntry = (@params.DocumentEntry, @params.DocumentNumber);
    }

    public void Dispose() {
        if (counting != null) {
            Marshal.ReleaseComObject(counting);
            counting = null;
        }

        if (service != null) {
            Marshal.ReleaseComObject(service);
            service = null;
        }

        if (companyService != null) {
            Marshal.ReleaseComObject(companyService);
            companyService = null;
        }

        if (rs != null) {
            Marshal.ReleaseComObject(rs);
            rs = null;
        }

        GC.Collect();
    }
}