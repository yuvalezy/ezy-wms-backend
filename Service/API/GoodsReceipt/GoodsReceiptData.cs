using System;
using System.Collections.Generic;
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

    public int CreateDocument(string name, int employeeID) {
        string query = string.Format(GetQuery("CreateGoodsReceipt"), name.ToQuery(), employeeID);
        return Global.DataObject.GetValue<int>(query);
    }

    public Document GetDocument(int id) {
        Document doc = null;
        var      sb  = new StringBuilder(GetQuery("GetGoodsReceipts"));
        sb.Append($" where DOCS.\"Code\" = {id}");
        Global.DataObject.ExecuteReader(sb, dr => {
            doc = new Document {
                ID             = (int)dr["ID"],
                Name           = (string)dr["Name"],
                Date           = (DateTime)dr["Date"],
                Employee       = new UserInfo((int)dr["EmployeeID"], (string)dr["EmployeeName"]),
                Status         = (DocumentStatus)Convert.ToChar(dr["Status"]),
                StatusDate     = (DateTime)dr["StatusDate"],
                StatusEmployee = new UserInfo((int)dr["StatusEmployeeID"], (string)dr["StatusEmployeeName"])
            };
        });
        return doc;
    }

    public IEnumerable<Document> GetDocuments(FilterParameters parameters) {
        List<Document> docs = new();
        var            sb   = new StringBuilder(GetQuery("GetGoodsReceipts"));
        if (parameters?.Statuses is { Length: > 0 }) {
            sb.Append(" and DOCS.\"U_Status\" in ('");
            sb.Append(string.Join("','", parameters.Statuses.Select(v => (char)v)));
            sb.Append("')");
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

        Global.DataObject.ExecuteReader(sb, dr => {
            while (dr.Read()) {
                var doc = new Document {
                    ID             = (int)dr["ID"],
                    Name           = (string)dr["Name"],
                    Date           = (DateTime)dr["Date"],
                    Employee       = new UserInfo((int)dr["EmployeeID"], (string)dr["EmployeeName"]),
                    Status         = (DocumentStatus)Convert.ToChar(dr["Status"]),
                    StatusDate     = (DateTime)dr["StatusDate"],
                    StatusEmployee = new UserInfo((int)dr["StatusEmployeeID"], (string)dr["StatusEmployeeName"])
                };
                docs.Add(doc);
            }
        });
        return docs;
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