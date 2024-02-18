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
        Global.DataObject.Execute(GetQuery("UpdateCountingStatus"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status)
        ]);
        Global.DataObject.Execute(GetQuery("UpdateCountingLineStatus"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status)
        ]);
    }

    private static void ProcessCountingSendAlert(int id, List<string> sendTo, CountingCreation creation) {
        try {
            using var alert = new Alert();
            alert.Subject = string.Format(ErrorMessages.WMSTransactionAlert, id);
            var transactionColumn = new AlertColumn(ErrorMessages.WMSTransaction);
            var documentColumn    = new AlertColumn(ErrorMessages.InventoryCounting, true);
            alert.Columns.AddRange(new[] { transactionColumn, documentColumn });
            transactionColumn.Values.Add(new AlertValue(creation.NewEntry.Entry.ToString()));
            documentColumn.Values.Add(new AlertValue(creation.NewEntry.Number.ToString(), "1470000065", creation.NewEntry.Entry.ToString()));
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
        return Global.DataObject.GetValue<int>(GetQuery("CreateCounting"), @params);
    }

    public int ValidateAddItem(AddItemParameter parameters, int employeeID) =>
        Global.DataObject.GetValue<int>(GetQuery("ValidateAddItemParameters"), [
            new Parameter("@ID", SqlDbType.Int, parameters.ID),
            new Parameter("@ItemCode", SqlDbType.NVarChar, 50, parameters.ItemCode),
            new Parameter("@BarCode", SqlDbType.NVarChar, 254, parameters.BarCode),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@BinEntry", SqlDbType.Int, parameters.BinEntry is > 0 ? parameters.BinEntry.Value : DBNull.Value),
        ]);

    public AddItemResponse AddItem(AddItemParameter parameters, int employeeID) {
        try {
            var returnValue = new AddItemResponse();
            Global.DataObject.BeginTransaction();
            Global.DataObject.ExecuteReader(GetQuery("AddItem"), [
                new Parameter("@ID", SqlDbType.Int, parameters.ID),
                new Parameter("@BinEntry", SqlDbType.Int, parameters.BinEntry is > 0 ? parameters.BinEntry.Value : DBNull.Value),
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
        List<Models.Counting> counts = [];
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
            new Parameter("@BinEntry", SqlDbType.Int, binEntry > 0 ? binEntry : DBNull.Value)
        ], dr => {
            list.Add(new() {
                Code     = (string)dr["ItemCode"],
                Name     = dr["ItemName"].ToString(),
                Quantity = Convert.ToInt32(dr["Quantity"])
            });
        });
        return list;
    }

    public int ValidateUpdateLine(UpdateLineParameter parameters) {
        return Global.DataObject.GetValue<int>(GetQuery("ValidateUpdateLineParameters"), [
            new Parameter("@ID", SqlDbType.Int, parameters.ID),
            new Parameter("@LineID", SqlDbType.Int, parameters.LineID),
            new Parameter("@Reason", SqlDbType.Int, parameters.CloseReason.HasValue ? parameters.CloseReason.Value : DBNull.Value)
        ]);
    }

    public void UpdateLine(UpdateLineParameter updateLineParameter) {
        var parameters = new Parameters {
            new Parameter("@ID", SqlDbType.Int) { Value     = updateLineParameter.ID },
            new Parameter("@LineID", SqlDbType.Int) { Value = updateLineParameter.LineID },
        };
        var  sb    = new StringBuilder("update \"@LW_YUVAL08_OINC1\" set ");
        bool comma = false;
        if (updateLineParameter.Comment != null) {
            sb.AppendLine("\"U_Comments\" = @Comments ");
            parameters.Add("@Comments", SqlDbType.NText).Value = updateLineParameter.Comment;
            comma                                              = true;
        }

        if (updateLineParameter.CloseReason.HasValue) {
            if (comma)
                sb.AppendLine(", ");
            sb.AppendLine("\"U_LineStatus\" = 'C', \"U_StatusReason\" = @Reason ");
            parameters.Add(new Parameter("@Reason", SqlDbType.Int) { Value = updateLineParameter.CloseReason.Value });
            comma = true;
        }

        if (updateLineParameter.Quantity.HasValue) {
            if (comma)
                sb.AppendLine(", ");
            sb.AppendLine("\"U_Quantity\" = @Quantity ");
            parameters.Add(new Parameter("@Quantity", SqlDbType.Int) { Value = updateLineParameter.Quantity.Value });
        }

        sb.AppendLine("where U_ID = @ID and \"U_LineID\" = @LineID");

        Global.DataObject.Execute(sb.ToString(), parameters);
    }
}