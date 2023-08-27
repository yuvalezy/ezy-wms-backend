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
        Global.DataObject.Execute(GetQuery("CancelGoodsReceipt"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@empID", SqlDbType.Int, employeeID),
        });
        return true;
    }

    public int CreateDocument(string cardCode, string name, int employeeID) =>
        Global.DataObject.GetValue<int>(GetQuery("CreateGoodsReceipt"), new Parameters {
            new Parameter("@Name", SqlDbType.NVarChar, 50, name),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@CardCode", SqlDbType.NVarChar, 50, cardCode),
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
        sb.Append($" where DOCS.\"U_WhsCode\" = '{parameters.WhsCode.ToQuery()}' ");
        if (parameters?.Statuses is { Length: > 0 }) {
            sb.Append(" and DOCS.\"U_Status\" in ('");
            sb.Append(string.Join("','", parameters.Statuses.Select(v => (char)v)));
            sb.Append("')");
        }

        if (parameters?.ID != null) {
            sb.Append($" and DOCS.\"Code\" = {parameters.ID} ");
        }

        if (parameters?.OrderBy != null) {
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

        Global.DataObject.ExecuteReader(sb, dr => docs.Add(ReadDocument(dr)));
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
        return doc;
    }

    private static string GetQuery(string id) {
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
}