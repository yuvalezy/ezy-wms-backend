using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Service.API.GoodsReceipt.Models;
using Service.API.Models;
using Service.Shared.Company;
using Service.Shared.Utils;

namespace Service.API.GoodsReceipt;

public class GoodsReceiptData {
    public bool CancelDocument(int id, int employeeID) {
        var doc = GetDocument(id);
        if (doc.Status is not (DocumentStatus.Open or DocumentStatus.InProgress))
            throw new Exception("Cannot cancel document if the Status is not Open or In Progress");
        string query = string.Format(GetQuery("CancelGoodsReceipt"), id, employeeID);
        Global.DataObject.Execute(query);
        return true;
    }

    public int CreateDocument(string cardCode, string name, int employeeID) {
        string query = string.Format(GetQuery("CreateGoodsReceipt"), name.ToQuery(), employeeID, cardCode.ToQuery());
        return Global.DataObject.GetValue<int>(query);
    }

    public Document GetDocument(int id) {
        Document doc = null;
        var      sb  = new StringBuilder(GetQuery("GetGoodsReceipts"));
        sb.Append($" where DOCS.\"Code\" = {id}");
        Global.DataObject.ExecuteReader(sb, dr => doc = ReadDocument(dr));
        return doc;
    }

    public IEnumerable<Document> GetDocuments(FilterParameters parameters) {
        List<Document> docs  = new();
        var            sb    = new StringBuilder(GetQuery("GetGoodsReceipts"));
        bool           where = false;
        if (parameters?.Statuses is { Length: > 0 }) {
            sb.Append(" where DOCS.\"U_Status\" in ('");
            sb.Append(string.Join("','", parameters.Statuses.Select(v => (char)v)));
            sb.Append("')");
            where = true;
        }

        if (parameters?.ID != null) {
            sb.Append(!where ? " where " : " and ");
            sb.Append($"DOCS.\"Code\" = {parameters.ID} ");
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
            BusinessPartner = new BusinessPartner((string)dr["CardCode"], dr["CardName"].ToString())
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