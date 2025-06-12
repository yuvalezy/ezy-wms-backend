using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.DTOs.InventoryCounting;
using Core.Enums;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class CountingCreation(
    SboCompany                                        sboCompany,
    int                                               countingNumber,
    string                                            whsCode,
    int                                               series,
    Dictionary<string, InventoryCountingCreationDataResponse> data,
    ILoggerFactory                                    loggerFactory)
    : IDisposable {
    private          CompanyService?            companyService;
    private          InventoryCountingsService? service;
    private          InventoryCounting?         counting;
    private          Recordset?                 rs;
    private readonly ILogger<CountingCreation>  logger = loggerFactory.CreateLogger<CountingCreation>();

    public (int Entry, int Number) NewEntry { get; private set; }

    public ProcessInventoryCountingResponse Execute() {
        var response = new ProcessInventoryCountingResponse();
        
        logger.LogInformation("Starting inventory counting creation for WMS counting {CountingNumber} in warehouse {Warehouse}", 
            countingNumber, whsCode);
        
        try {
            sboCompany.TransactionMutex.WaitOne();
            
            try {
                sboCompany.ConnectCompany();
                sboCompany.Company!.StartTransaction();
                
                CreateCounting();
                
                if (sboCompany.Company.InTransaction) {
                    sboCompany.Company.EndTransaction(BoWfTransOpt.wf_Commit);
                }
                
                response.Success        = true;
                response.ExternalEntry  = NewEntry.Entry;
                response.ExternalNumber = NewEntry.Number;
                response.Status         = ResponseStatus.Ok;
                
                logger.LogInformation("Successfully created SAP B1 inventory counting {DocNumber} (Entry: {DocEntry}) for WMS counting {CountingNumber}", 
                    NewEntry.Number, NewEntry.Entry, countingNumber);
            }
            finally {
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to create inventory counting for WMS counting {CountingNumber}", countingNumber);
            
            if (sboCompany.Company?.InTransaction == true) {
                sboCompany.Company.EndTransaction(BoWfTransOpt.wf_RollBack);
            }
            
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
        
        int totalLines = 0;
        foreach (var value in data) {
            if (value.Value.CountedBins.Count > 0) {
                logger.LogDebug("Processing item {ItemCode} with {BinCount} bins", 
                    value.Value.ItemCode, value.Value.CountedBins.Count);
                
                foreach (var countedBin in value.Value.CountedBins) {
                    var line = counting.InventoryCountingLines.Add();
                    line.ItemCode        = value.Value.ItemCode;
                    line.WarehouseCode   = whsCode;
                    line.BinEntry        = countedBin.BinEntry;
                    line.Counted         = BoYesNoEnum.tYES;
                    line.CountedQuantity = countedBin.CountedQuantity;
                    totalLines++;
                    
                    logger.LogDebug("Added counting line for item {ItemCode} in bin {BinEntry} with quantity {Quantity} (system: {SystemQuantity})", 
                        value.Value.ItemCode, countedBin.BinEntry, countedBin.CountedQuantity, countedBin.SystemQuantity);
                }
            }
            else {
                
                var line = counting.InventoryCountingLines.Add();
                line.ItemCode        = value.Value.ItemCode;
                line.WarehouseCode   = whsCode;
                line.Counted         = BoYesNoEnum.tYES;
                line.CountedQuantity = value.Value.CountedQuantity;
                totalLines++;
                
                logger.LogDebug("Added counting line for item {ItemCode} with quantity {Quantity} (system: {SystemQuantity}, variance: {Variance})", 
                    value.Value.ItemCode, value.Value.CountedQuantity, value.Value.SystemQuantity, value.Value.Variance);
            }
        }

        logger.LogInformation("Calling SAP B1 counting service.Add() with {LineCount} lines...", totalLines);
        var @params = service.Add(counting);
        
        NewEntry = (@params.DocumentEntry, @params.DocumentNumber);
        logger.LogDebug("Inventory counting created successfully with DocNum: {DocNum} and DocEntry: {DocEntry}", 
            @params.DocumentNumber, @params.DocumentEntry);
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