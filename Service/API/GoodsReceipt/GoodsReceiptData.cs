using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Service.API.GoodsReceipt.Models;
using Service.API.Models;
using Service.Shared.Company;
using Service.Shared.Data;
using Service.Shared.Utils;

namespace Service.API.GoodsReceipt;

public class GoodsReceiptData {
    public bool CancelDocument(int id, int employeeID) {
        var doc = GetDocument(id);
        if (doc.Status is not (DocumentStatus.Open or DocumentStatus.InProgress))
            throw new Exception("Cannot cancel document if the Status is not Open or In Progress");
        UpdateDocumentStatus(id, employeeID, DocumentStatus.Cancelled);
        return true;
    }

    public bool ProcessDocument(int id, int employeeID) {
        var doc = GetDocument(id);
        if (doc.Status != DocumentStatus.InProgress)
            throw new Exception("Cannot process document if the Status is not In Progress");
        UpdateDocumentStatus(id, employeeID, DocumentStatus.Processing);
        try {
            using var creation = new GoodsReceiptCreation(id, employeeID);
            creation.Execute();
            UpdateDocumentStatus(id, employeeID, DocumentStatus.Finished);
            return true;
        }
        catch (Exception e) {
            UpdateDocumentStatus(id, employeeID, DocumentStatus.InProgress);
            throw;
        }
    }

    public int CreateDocument(string cardCode, string name, int employeeID) =>
        Global.DataObject.GetValue<int>(GetQuery("CreateGoodsReceipt"), new Parameters {
            new Parameter("@Name", SqlDbType.NVarChar, 50, name),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@CardCode", SqlDbType.NVarChar, 50, cardCode),
        });

    public int ValidateAddItem(int id, string itemCode, string barCode) =>
        Global.DataObject.GetValue<int>(GetQuery("ValidateAddItemParameters"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@ItemCode", SqlDbType.NVarChar, 50, itemCode),
            new Parameter("@BarCode", SqlDbType.NVarChar, 254, barCode),
        });

    public AddItemReturnValue AddItem(int id, string itemCode, string barcode, int employeeID) {
        int returnValue;
        try {
            Global.DataObject.BeginTransaction();
            returnValue = Global.DataObject.GetValue<int>(GetQuery("AddItem"), new Parameters {
                new Parameter("@ID", SqlDbType.Int, id),
                new Parameter("@ItemCode", SqlDbType.NVarChar, 50, itemCode),
                new Parameter("@BarCode", SqlDbType.NVarChar, 254, barcode),
                new Parameter("@empID", SqlDbType.Int, employeeID),
            });
            Global.DataObject.CommitTransaction();
        }
        catch {
            Global.DataObject.RollbackTransaction();
            throw;
        }

        return (AddItemReturnValue)returnValue;
    }

    public Document GetDocument(int id) {
        Document doc = null;
        var      sb  = new StringBuilder(GetQuery("GetGoodsReceipts"));
        sb.Append(" where DOCS.\"Code\" = @ID");
        Global.DataObject.ExecuteReader(sb, new Parameter("@ID", SqlDbType.Int, id), dr => doc = ReadDocument(dr));
        return doc;
    }

    public IEnumerable<Document> GetDocuments(FilterParameters parameters) {
        List<Document> docs = new();
        var            sb   = new StringBuilder(GetQuery("GetGoodsReceipts"));
        var queryParams = new Parameters() {
            new Parameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = parameters.WhsCode }
        };
        sb.Append($" where DOCS.\"U_WhsCode\" = @WhsCode ");
        if (parameters.Status is { Length: > 0 }) {
            sb.Append(" and DOCS.\"U_Status\" in ('");
            sb.Append(string.Join("','", parameters.Status.Select(v => (char)v)));
            sb.Append("')");
        }

        if (parameters.ID != null) {
            queryParams.Add("@Code", SqlDbType.Int).Value = parameters.ID;
            sb.Append(" and DOCS.\"Code\" = @Code ");
        }

        if (parameters.Name != null) {
            queryParams.Add("@Name", SqlDbType.NVarChar, 50).Value = parameters.Name;
            sb.Append(" and DOCS.\"Name\" = @Name ");
        }

        if (parameters.BusinessPartner != null) {
            queryParams.Add("@CardCode", SqlDbType.NVarChar, 50).Value = parameters.BusinessPartner;
            sb.Append(" and DOCS.\"U_CardCode\" = @CardCode ");
        }

        if (parameters.Date != null) {
            queryParams.Add("@Date", SqlDbType.DateTime).Value = parameters.Date;
            sb.Append(" and DATEDIFF(day,DOCS.\"U_StatusDate\",@Date) = 0 ");
        }

        if (parameters.GRPO != null) {
            queryParams.Add("@GRPO", SqlDbType.Int).Value = parameters.GRPO;
            sb.Append(" and OPDN.\"DocNum\" = @GRPO ");
        }

        if (parameters.OrderBy != null) {
            sb.Append(" order by DOCS.");
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

        Global.DataObject.ExecuteReader(sb.ToString(), queryParams, dr => docs.Add(ReadDocument(dr)));
        return docs;
    }

    private static Document ReadDocument(IDataReader dr) {
        var doc = new Document {
            ID              = (int)dr["ID"],
            Name            = (string)dr["Name"],
            Date            = (DateTime)dr["Date"],
            Employee        = new UserInfo((int)dr["EmployeeID"], (string)dr["EmployeeName"]),
            Status          = (DocumentStatus)Convert.ToChar(dr["Status"]),
            StatusDate      = (DateTime)dr["StatusDate"],
            StatusEmployee  = new UserInfo((int)dr["StatusEmployeeID"], (string)dr["StatusEmployeeName"]),
            BusinessPartner = new BusinessPartner((string)dr["CardCode"], dr["CardName"].ToString()),
            WhsCode         = (string)dr["WhsCode"]
        };
        if (dr["GRPO"] != DBNull.Value)
            doc.GRPO = (int)dr["GRPO"];
        return doc;
    }

    public static string GetQuery(string id) {
        string resourceName = $"Service.API.GoodsReceipt.Queries.{ConnectionController.DatabaseType}.{id}.sql";
        var    assembly     = typeof(Queries).Assembly;
        string resourcePath = resourceName;

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null) {
            throw new ArgumentException($"Specified resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void UpdateDocumentStatus(int id, int employeeID, DocumentStatus status) {
        Global.DataObject.Execute(GetQuery("UpdateGoodsReceiptStatus"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status),
        });
        Global.DataObject.Execute(GetQuery("UpdateGoodsReceiptLineStatus"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status),
        });
    }

    public List<GoodsReceiptVSExitReport> GetGoodsReceiptVSExitReport(int id) {
        var data    = new List<GoodsReceiptVSExitReport>();
        var control = new Dictionary<(int, int), GoodsReceiptVSExitReport>();
        Global.DataObject.ExecuteReader(GetQuery("GoodsReceiptVSExit"), new Parameter("@ID", SqlDbType.Int) { Value = id }, dr => {
            int objectType = (int)dr["ObjType"];
            int docNum     = (int)dr["DocNum"];
            var tuple      = (objectType, docNum);

            GoodsReceiptVSExitReport value;
            if (!control.ContainsKey(tuple)) {
                value = new GoodsReceiptVSExitReport(objectType, docNum, dr["CardName"].ToString(), dr["Address2"].ToString());
                control.Add(tuple, value);
                data.Add(value);
            }
            else {
                value = control[tuple];
            }

            value.Lines.Add(new GoodsReceiptVSExitReportLine((string)dr["ItemCode"], dr["ItemName"].ToString(), (int)dr["OpenInvQty"], (int)dr["Quantity"]));

        });
        return data;
    }
}
