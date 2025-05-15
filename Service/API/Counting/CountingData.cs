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
        var tracer = Global.Debug ? new ServiceTracer(MethodType.Post, "ProcessCounting") : null;
        tracer?.Write($"Get Counting {id}");
        var doc = GetCounting(id);
        if (doc.Status != DocumentStatus.InProgress) {
            const string errorMessage = "Cannot process counting if the Status is not In Progress";
            tracer?.Write($"Error: {errorMessage}");
            throw new Exception(errorMessage);
        }

        tracer?.Write("Update Counting Status Processing");
        UpdateCountingStatus(id, employeeID, DocumentStatus.Processing);
        try {
            tracer?.Write("Create Inventory Counting");
            using var creation = new CountingCreation(id, employeeID, tracer);
            tracer?.Write("Executing Inventory Counting");
            creation.Execute();
            tracer?.Write("Update Counting Status Finished");
            UpdateCountingStatus(id, employeeID, DocumentStatus.Finished);
            tracer?.Write("Set Closed Lines");
            creation.SetClosedLines();
            tracer?.Write("Process Counting Send Alert");
            ProcessCountingSendAlert(id, sendTo, creation);
            return true;
        }
        catch (Exception e) {
            tracer?.Write("Error occured: " + e.Message);
            tracer?.Write("Restoring In Progress Status");
            UpdateCountingStatus(id, employeeID, DocumentStatus.InProgress);
            throw;
        }
    }

    private static void UpdateCountingStatus(int id, int employeeID, DocumentStatus status) {
        using var conn = Global.Connector;
        conn.Execute(GetQuery("UpdateCountingStatus"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status)
        ]);
        conn.Execute(GetQuery("UpdateCountingLineStatus"), [
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
        using var conn = Global.Connector;
        return conn.GetValue<int>(GetQuery("CreateCounting"), @params);
    }

    public int ValidateAddItem(DataConnector conn, AddItemParameter parameters, int employeeID) =>
        conn.GetValue<int>(GetQuery("ValidateAddItemParameters"), [
            new Parameter("@ID", SqlDbType.Int, parameters.ID),
            new Parameter("@ItemCode", SqlDbType.NVarChar, 50, parameters.ItemCode),
            new Parameter("@BarCode", SqlDbType.NVarChar, 254, parameters.BarCode),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@BinEntry", SqlDbType.Int, parameters.BinEntry is > 0 ? parameters.BinEntry.Value : DBNull.Value),
        ]);

    public AddItemResponse AddItem(DataConnector conn, AddItemParameter parameters, int employeeID) {
        var returnValue = new AddItemResponse();
        conn.ExecuteReader(GetQuery("AddItem"), [
            new Parameter("@ID", SqlDbType.Int, parameters.ID),
            new Parameter("@BinEntry", SqlDbType.Int, parameters.BinEntry is > 0 ? parameters.BinEntry.Value : DBNull.Value),
            new Parameter("@ItemCode", SqlDbType.NVarChar, 50, parameters.ItemCode),
            new Parameter("@BarCode", SqlDbType.NVarChar, 254, parameters.BarCode),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@Quantity", SqlDbType.Int, parameters.Quantity),
            new Parameter("@Unit", SqlDbType.SmallInt, parameters.Unit)
        ], dr => {
            returnValue.LineID   = (int)dr["LineID"];
            returnValue.Unit     = parameters.Unit;
            returnValue.NumIn    = Convert.ToInt32(dr["NumInBuy"]);
            returnValue.UnitMsr  = dr["BuyUnitMsr"].ToString();
            returnValue.PackUnit = Convert.ToInt32(dr["PurPackUn"]);
            returnValue.PackMsr  = dr["PurPackMsr"].ToString();
        });
        return returnValue;
    }

    public Models.Counting GetCounting(int id) {
        Models.Counting count = null;
        var             sb    = new StringBuilder(GetQuery("GetCountings"));
        sb.Append(" where COUNTS.\"Code\" = @ID");
        using var conn = Global.Connector;
        conn.ExecuteReader(sb, new Parameter("@ID", SqlDbType.Int, id), dr => count = ReadCounting(dr));
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

        using var conn = Global.Connector;
        conn.ExecuteReader(query, queryParams, dr => counts.Add(ReadCounting(dr)));
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
        var       list = new List<CountingContent>();
        using var conn = Global.Connector;
        conn.ExecuteReader(GetQuery("CountingContent"), [
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

    public int ValidateUpdateLine(DataConnector conn, UpdateLineParameter parameters) {
        return conn.GetValue<int>(GetQuery("ValidateUpdateLineParameters"), [
            new Parameter("@ID", SqlDbType.Int, parameters.ID),
            new Parameter("@LineID", SqlDbType.Int, parameters.LineID),
            new Parameter("@Reason", SqlDbType.Int, parameters.CloseReason.HasValue ? parameters.CloseReason.Value : DBNull.Value)
        ]);
    }

    public void UpdateLine(DataConnector conn, UpdateLineParameter updateLineParameter) {
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

        conn.Execute(sb.ToString(), parameters);
    }

    public CountingSummary GetCountingSummaryReport(int id) {
        var          value     = new CountingSummary();
        using var    conn      = Global.Connector;
        const string headerStr = "select \"Name\" from \"@LW_YUVAL08_OINC\" where \"Code\" = @ID";
        var          idParam   = new Parameter("@ID", SqlDbType.Int) { Value = id };
        value.Name = conn.GetValue<string>(headerStr, idParam);

        const string sqlStr = """
                                SELECT c."BinCode", a."U_ItemCode" "ItemCode", b."ItemName", SUM(a."U_Quantity") "Quantity"
                                FROM "@LW_YUVAL08_OINC1" a
                                         inner join OITM b on b."ItemCode" = a."U_ItemCode"
                                       inner join OBIN c on c."AbsEntry" = a."U_BinEntry"
                                WHERE a.U_ID = @ID AND a."U_LineStatus" <> 'C'
                                GROUP BY c."BinCode", a."U_ItemCode", b."ItemName"
                                order by 1
                              """;
        conn.ExecuteReader(sqlStr, idParam,
            dr => { value.Lines.Add(new CountingSummaryLine((string)dr["BinCode"], (string)dr["ItemCode"], dr["ItemName"].ToString(), Convert.ToDouble(dr["Quantity"]))); });
        return value;
    }
}