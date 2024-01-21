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

internal class CountingCreation(int id, int employeeID) : IDisposable {
    private string                       whsCode;
    private CompanyService               companyService;
    private InventoryCountingsService    service;
    private InventoryCounting            counting;
    private Recordset                    rs;
    private IEnumerable<CountingContent> data;

    public (int Entry, int Number) NewEntry { get; private set; }

    public void Execute() {
        bool releaseMutex = false;
        try {
            LoadData();
            int docSeries = GeneralData.GetSeries(ObjectTypes.oInventoryCounting);
            Global.TransactionMutex.WaitOne();
            releaseMutex = true;
            ConnectionController.BeginTransaction();
            Global.ConnectCompany();
            CreateCounting(docSeries);
            ConnectionController.Commit();
        }
        catch (Exception e) {
            ConnectionController.TryRollback();
            throw new Exception("Error generating Counting: " + e.Message);
        }
        finally {
            if (releaseMutex)
                Global.TransactionMutex.ReleaseMutex();
        }
    }

    private void CreateCounting(int series) {
        //todo lower default bin location quantity
        companyService  = ConnectionController.Company.GetCompanyService();
        service         = (InventoryCountingsService)companyService.GetBusinessService(ServiceTypes.InventoryCountingsService);
        counting        = (InventoryCounting)service.GetDataInterface(InventoryCountingsServiceDataInterfaces.icsInventoryCounting);
        counting.Series = series;
        foreach (var value in data) {
            var line = counting.InventoryCountingLines.Add();
            line.ItemCode      = value.Code;
            line.WarehouseCode = whsCode;
            if (value.BinEntry > 0) {
                line.BinEntry = value.BinEntry.Value;
            }

            line.Counted         = BoYesNoEnum.tYES;
            line.CountedQuantity = value.Quantity;
        }
        var @params = service.Add(counting);
        NewEntry = (@params.DocumentEntry, @params.DocumentNumber);
    }

    private void LoadData() {
        const string query = """select "U_WhsCode" "WhsCode" from "@LW_YUVAL08_OINC" where "Code" = @ID""";
        whsCode = Global.DataObject.GetValue<string>(query, new Parameter("@ID", SqlDbType.Int, id));
        using var dt = Global.DataObject.GetDataTable(CountingData.GetQuery("ProcessCountingLines"), [
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
        string sqlStr = "update \"@LW_YUVAL08_OINC1\" set \"U_LineStatus\" = 'C' where U_ID = @ID and \"U_LineStatus\" = 'O'";
        Global.DataObject.Execute(sqlStr, new Parameter("@ID", SqlDbType.Int, id));
    }
}