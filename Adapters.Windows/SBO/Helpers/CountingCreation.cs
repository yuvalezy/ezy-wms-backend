using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.DTOs;
using Core.Models;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class CountingCreation(
    SboDatabaseService                                dbService,
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
    private readonly ILogger<CountingCreation>  tracer = new Logger<CountingCreation>(loggerFactory) ;

    public (int Entry, int Number) NewEntry { get; private set; }

    public ProcessInventoryCountingResponse Execute() {
        try {
            tracer?.Write("Wait Mutex");
            sboCompany.TransactionMutex.WaitOne();
            try {
                tracer?.Write("Checking Company Connection");
                sboCompany.ConnectCompany();
                company.StartTransaction();
                tracer?.Write("Begin Transaction");
                ConnectionController.BeginTransaction();
                tracer?.Write("Creating Counting Object");
                CreateCounting(series);
                tracer?.Write("Commiting");
                ConnectionController.Commit();
            }
            finally {
                tracer?.Write("Release Mutex");
                Global.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception e) {
            ConnectionController.TryRollback();
            string errorMessage = "Error generating Counting: " + e.Message;
            tracer?.Write(errorMessage);
            throw new Exception(errorMessage);
        }
    }

    private void CreateCounting(int series) {
        //todo lower default bin location quantity
        tracer?.Write("Get Company Service");
        companyService = ConnectionController.Company.GetCompanyService();
        tracer?.Write("Get Service");
        service = (InventoryCountingsService)companyService.GetBusinessService(ServiceTypes.InventoryCountingsService);
        tracer?.Write("Get Counting Service");
        counting = (InventoryCounting)service.GetDataInterface(InventoryCountingsServiceDataInterfaces.icsInventoryCounting);
        tracer?.Write("Assigning Counting Series");
        counting.Series = series;
        tracer?.Write($"Counted Data: {data.Count()}");
        foreach (var value in data) {
            var line = counting.InventoryCountingLines.Add();
            tracer?.Write($"Processing Line {line.LineNumber}, Code: {value.Code}, Whs: {whsCode}, bin: {value.BinEntry}");
            line.ItemCode      = value.Code;
            line.WarehouseCode = whsCode;
            if (value.BinEntry > 0) {
                line.BinEntry = value.BinEntry.Value;
            }

            line.Counted         = BoYesNoEnum.tYES;
            line.CountedQuantity = value.Quantity;
        }

        tracer?.Write("Adding Counting through service");
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


    public void SetClosedLines() {
        string    sqlStr = "update \"@LW_YUVAL08_OINC1\" set \"U_LineStatus\" = 'C' where U_ID = @ID and \"U_LineStatus\" = 'O'";
        using var conn   = Global.Connector;
        conn.Execute(sqlStr, new Parameter("@ID", SqlDbType.Int, id));
    }
}