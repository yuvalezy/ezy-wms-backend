using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using SAPbobsCOM;
using Service.API.Counting.Models;
using Service.Shared;
using Service.Shared.Company;
using Service.Shared.Data;
using GeneralData = Service.API.General.GeneralData;

namespace Service.API.Counting;

internal class CountingCreation(int id, int employeeID, ServiceTracer tracer) : IDisposable {
    private string                       whsCode;
    private CompanyService               companyService;
    private InventoryCountingsService    service;
    private InventoryCounting            counting;
    private Recordset                    rs;
    private IEnumerable<CountingContent> data;

    public (int Entry, int Number) NewEntry { get; private set; }

    public void Execute() {
        try {
            tracer?.Write("Wait Mutex");
            if (!Global.TransactionMutex.WaitOne()) 
                return;
            try {
                tracer?.Write("Loading Data");
                LoadData();
                tracer?.Write("Getting Document Series");
                int docSeries = GeneralData.GetSeries(ObjectTypes.oInventoryCounting);
                tracer?.Write("Checking Company Connection");
                Global.ConnectCompany();
                tracer?.Write("Begin Transaction");
                ConnectionController.BeginTransaction();
                tracer?.Write("Creating Counting Object");
                CreateCounting(docSeries);
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
        companyService  = ConnectionController.Company.GetCompanyService();
        tracer?.Write("Get Service");
        service         = (InventoryCountingsService)companyService.GetBusinessService(ServiceTypes.InventoryCountingsService);
        tracer?.Write("Get Counting Service");
        counting        = (InventoryCounting)service.GetDataInterface(InventoryCountingsServiceDataInterfaces.icsInventoryCounting);
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

    private void LoadData() {
        const string query = """select "U_WhsCode" "WhsCode" from "@LW_YUVAL08_OINC" where "Code" = @ID""";
        using var    conn  = Global.Connector;
        whsCode = conn.GetValue<string>(query, new Parameter("@ID", SqlDbType.Int, id));
        using var dt = conn.GetDataTable(CountingData.GetQuery("ProcessCountingLines"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@WhsCode", SqlDbType.NVarChar, 8, whsCode),
        ]);
        data = dt.Rows.Cast<DataRow>()
            .Select(dr => new CountingContent {
                Code     = (string)dr["ItemCode"],
                Quantity = Convert.ToInt32(dr["Quantity"]),
                BinEntry = dr["BinEntry"] != DBNull.Value ? (int)dr["BinEntry"] : null
            });
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