using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Service.API.Counting.Models;
using Service.API.General;
using Service.API.Models;
using Service.Shared.Company;
using Service.Shared.Data;

namespace Service.API.Counting;

public class CountingData {
    public bool CancelCounting(int id, int employeeID) {
        var doc = GetCounting(id);
        if (doc.Status is not (DocumentStatus.Open or DocumentStatus.InProgress))
            throw new Exception("Cannot cancel counting if the Status is not Open or In Progress");
        UpdateCountingStatus(id, employeeID, DocumentStatus.Cancelled);
        return true;
    }

    public bool ProcessCounting(int id, int employeeID, List<string> sendTo) {
        var doc = GetCounting(id);
        if (doc.Status != DocumentStatus.InProgress)
            throw new Exception("Cannot process counting if the Status is not In Progress");
        UpdateCountingStatus(id, employeeID, DocumentStatus.Processing);
        try {
            using var creation = new CountingCreation(id, employeeID);
            creation.Execute();
            UpdateCountingStatus(id, employeeID, DocumentStatus.Finished);
            creation.SetClosedLines();
            ProcessCountingSendAlert(id, sendTo, creation);
            return true;
        }
        catch (Exception e) {
            UpdateCountingStatus(id, employeeID, DocumentStatus.InProgress);
            throw;
        }
    }

    private static void UpdateCountingStatus(int id, int employeeID, DocumentStatus status) {
        Global.DataObject.Execute(GetQuery("UpdateCountingStatus"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status),
        });
        Global.DataObject.Execute(GetQuery("UpdateCountingLineStatus"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status),
        });
    }

    private static void ProcessCountingSendAlert(int id, List<string> sendTo, CountingCreation creation) {
        try {
            using var alert = new Alert();
            alert.Subject = string.Format(ErrorMessages.WMSTransactionAlert, id);
            var transactionColumn = new AlertColumn(ErrorMessages.WMSTransaction);
            var documentColumn    = new AlertColumn(ErrorMessages.InventoryCounting, true);
            alert.Columns.AddRange(new[] { transactionColumn, documentColumn });
            transactionColumn.Values.Add(new AlertValue(creation.Entry.ToString()));
            documentColumn.Values.Add(new AlertValue(creation.Number.ToString(), "1470000065", creation.Entry.ToString()));
            alert.Send(sendTo);
        }
        catch (Exception e) {
            //todo log error handler
        }
    }

    public int CreateCounting(CreateParameters parameters, int employeeID) {
        var @params = new Parameters {
            new Parameter("@Name", SqlDbType.NVarChar, 50, parameters.Name),
            new Parameter("@empID", SqlDbType.Int, employeeID),
        };
        int id = Global.DataObject.GetValue<int>(GetQuery("CreateCounting"), @params);
        return id;
    }

    public int ValidateAddItem(int id, string itemCode, string barCode, int empID) =>
        Global.DataObject.GetValue<int>(GetQuery("ValidateAddItemParameters"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@ItemCode", SqlDbType.NVarChar, 50, itemCode),
            new Parameter("@BarCode", SqlDbType.NVarChar, 254, barCode),
            new Parameter("@empID", SqlDbType.Int, empID)
        ]);

    public AddItemResponse AddItem(AddItemParameter parameters, int employeeID) {
        try {
            var returnValue = new AddItemResponse();
            Global.DataObject.BeginTransaction();
            Global.DataObject.ExecuteReader(GetQuery("AddItem"), [
                new Parameter("@ID", SqlDbType.Int, parameters.ID),
                new Parameter("@BinEntry", SqlDbType.Int, parameters.BinEntry.HasValue ? parameters.BinEntry.Value : DBNull.Value),
                new Parameter("@ItemCode", SqlDbType.NVarChar, 50, parameters.ItemCode),
                new Parameter("@BarCode", SqlDbType.NVarChar, 254, parameters.BarCode),
                new Parameter("@empID", SqlDbType.Int, employeeID),
                new Parameter("@Quantity", SqlDbType.Int, parameters.Quantity)
            ], dr => returnValue.LineID = (int)dr["LineID"]);
            Global.DataObject.CommitTransaction();
            return returnValue;
        }
        catch {
            Global.DataObject.RollbackTransaction();
            throw;
        }
    }

    public Models.Counting GetCounting(int id) {
        Models.Counting count = null;
        var             sb    = new StringBuilder(GetQuery("GetCountings"));
        sb.Append(" where COUNTS.\"Code\" = @ID");
        Global.DataObject.ExecuteReader(sb, new Parameter("@ID", SqlDbType.Int, id), dr => count = ReadCounting(dr));
        return count;
    }

    public IEnumerable<Models.Counting> GetCountings(FilterParameters parameters) {
        List<Models.Counting> counts = new();
        var                   sb     = new StringBuilder(GetQuery("GetCountings"));
        var queryParams = new Parameters {
            new Parameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = parameters.WhsCode }
        };
        sb.Append($" where COUNTS.\"U_WhsCode\" = @WhsCode ");
        if (parameters.Status is { Length: > 0 }) {
            sb.Append(" and COUNTS.\"U_Status\" in ('");
            sb.Append(string.Join("','", parameters.Status.Select(v => (char)v)));
            sb.Append("')");
        }

        if (parameters.ID != null) {
            queryParams.Add("@Code", SqlDbType.Int).Value = parameters.ID;
            sb.Append(" and COUNTS.\"Code\" = @Code ");
        }

        if (parameters.Name != null) {
            queryParams.Add("@Name", SqlDbType.NVarChar, 50).Value = parameters.Name;
            sb.Append(" and COUNTS.\"Name\" = @Name ");
        }

        if (parameters.Date != null) {
            queryParams.Add("@Date", SqlDbType.DateTime).Value = parameters.Date;
            sb.Append(" and DATEDIFF(day,COUNTS.\"U_StatusDate\",@Date) = 0 ");
        }

        if (parameters.OrderBy != null) {
            sb.Append(" order by COUNTS.");
            switch (parameters.OrderBy) {
                case OrderBy.ID:
                    sb.Append("Code");
                    break;
                case OrderBy.Name:
                    sb.Append("Name");
                    break;
                case OrderBy.Date:
                    sb.Append("U_Date");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (parameters.Desc)
                sb.Append(" desc");
        }

        string query = sb.ToString();

        Global.DataObject.ExecuteReader(query, queryParams, dr => counts.Add(ReadCounting(dr)));
        var documents = counts.ToArray();
        return documents;
    }

    private static Models.Counting ReadCounting(IDataRecord dr) {
        var count = new Models.Counting {
            ID             = (int)dr["ID"],
            Name           = (string)dr["Name"],
            Date           = (DateTime)dr["Date"],
            Employee       = new UserInfo((int)dr["EmployeeID"], (string)dr["EmployeeName"]),
            Status         = (DocumentStatus)Convert.ToChar(dr["Status"]),
            StatusDate     = (DateTime)dr["StatusDate"],
            StatusEmployee = new UserInfo((int)dr["StatusEmployeeID"], (string)dr["StatusEmployeeName"]),
            WhsCode        = (string)dr["WhsCode"],
        };
        return count;
    }

    public static string GetQuery(string id) {
        string resourceName = $"Service.API.Counting.Queries.{ConnectionController.DatabaseType}.{id}.sql";
        var    assembly     = typeof(Queries).Assembly;
        string resourcePath = resourceName;

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null) {
            throw new ArgumentException($"Specified resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public IEnumerable<CountingContent> GetCountingContent(int id, int binEntry) {
        var list = new List<CountingContent>();
        Global.DataObject.ExecuteReader(GetQuery("CountingContent"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@BinEntry", SqlDbType.Int, binEntry <= 0 ? binEntry : DBNull.Value)
        ], dr => {
            list.Add(new() {
                Code     = (string)dr["ItemCode"],
                Name     = dr["ItemName"].ToString(),
                Quantity = Convert.ToInt32(dr["Quantity"])
            });
        });
        return list;
    }
}