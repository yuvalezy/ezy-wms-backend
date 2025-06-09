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
        logger.LogDebug("Counting data contains {ItemCount} items with series {Series}", data.Count, series);
        
        try {
            logger.LogDebug("Waiting for transaction mutex...");
            sboCompany.TransactionMutex.WaitOne();
            
            try {
                logger.LogDebug("Connecting to SAP B1 company...");
                sboCompany.ConnectCompany();
                
                logger.LogDebug("Starting SAP B1 transaction...");
                sboCompany.Company!.StartTransaction();
                
                CreateCounting();
                
                if (sboCompany.Company.InTransaction) {
                    logger.LogDebug("Committing SAP B1 transaction...");
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
                logger.LogDebug("Releasing transaction mutex");
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to create inventory counting for WMS counting {CountingNumber}", countingNumber);
            
            if (sboCompany.Company?.InTransaction == true) {
                logger.LogDebug("Rolling back SAP B1 transaction due to error");
                sboCompany.Company.EndTransaction(BoWfTransOpt.wf_RollBack);
            }
            
            response.ErrorMessage = e.Message;
            response.Status       = ResponseStatus.Error;
        }

        return response;
    }

    private void CreateCounting() {
        logger.LogDebug("Getting company service for inventory counting...");
        companyService      = sboCompany.Company!.GetCompanyService();
        
        logger.LogDebug("Getting inventory countings service...");
        service             = (InventoryCountingsService)companyService.GetBusinessService(ServiceTypes.InventoryCountingsService);
        
        logger.LogDebug("Creating inventory counting data interface...");
        counting            = (InventoryCounting)service.GetDataInterface(InventoryCountingsServiceDataInterfaces.icsInventoryCounting);
        
        counting.Series     = series;
        counting.Reference2 = countingNumber.ToString();
        logger.LogDebug("Set counting reference2 to WMS counting number: {CountingNumber}", countingNumber);
        
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
                logger.LogDebug("Processing item {ItemCode} without bins", value.Value.ItemCode);
                
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
        logger.LogDebug("Disposing CountingCreation resources...");
        
        if (counting != null) {
            logger.LogDebug("Releasing InventoryCounting COM object");
            Marshal.ReleaseComObject(counting);
            counting = null;
        }

        if (service != null) {
            logger.LogDebug("Releasing InventoryCountingsService COM object");
            Marshal.ReleaseComObject(service);
            service = null;
        }

        if (companyService != null) {
            logger.LogDebug("Releasing CompanyService COM object");
            Marshal.ReleaseComObject(companyService);
            companyService = null;
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