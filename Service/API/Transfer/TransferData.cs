using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Service.API.General.Models;
using Service.API.Models;
using Service.API.Transfer.Models;
using Service.Shared.Company;
using Service.Shared.Data;

namespace Service.API.Transfer;

public class TransferData {
    public int CreateTransfer(CreateParameters parameters, int employeeID) {
        var @params = new Parameters {
            new Parameter("@empID", SqlDbType.Int, employeeID),
        };
        return Global.DataObject.GetValue<int>(GetQuery("CreateTransfer"), @params);
    }

    public Models.Transfer GetTransfer(int id) {
        Models.Transfer count = null;
        var             sb    = new StringBuilder(GetQuery("GetTransfers"));
        sb.Append(" where TRANSFERS.\"Code\" = @ID");
        Global.DataObject.ExecuteReader(sb, new Parameter("@ID", SqlDbType.Int, id), dr => count = ReadTransfer(dr));
        return count;
    }

    public IEnumerable<Models.Transfer> GetTransfers(FilterParameters parameters) {
        List<Models.Transfer> counts = [];
        var                   sb     = new StringBuilder(GetQuery("GetTransfers"));
        var queryParams = new Parameters {
            new Parameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = parameters.WhsCode }
        };
        sb.Append($" where TRANSFERS.\"U_WhsCode\" = @WhsCode ");
        if (parameters.Status is { Length: > 0 }) {
            sb.Append(" and TRANSFERS.\"U_Status\" in ('");
            sb.Append(string.Join("','", parameters.Status.Select(v => (char)v)));
            sb.Append("')");
        }

        if (parameters.ID != null) {
            queryParams.Add("@Code", SqlDbType.Int).Value = parameters.ID;
            sb.Append(" and TRANSFERS.\"Code\" = @Code ");
        }

        if (parameters.Date != null) {
            queryParams.Add("@Date", SqlDbType.DateTime).Value = parameters.Date;
            sb.Append(" and DATEDIFF(day,TRANSFERS.\"U_StatusDate\",@Date) = 0 ");
        }

        if (parameters.OrderBy != null) {
            sb.Append(" order by TRANSFERS.");
            switch (parameters.OrderBy) {
                case OrderBy.ID:
                    sb.Append("Code");
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

        Global.DataObject.ExecuteReader(query, queryParams, dr => counts.Add(ReadTransfer(dr)));
        var documents = counts.ToArray();
        return documents;
    }
    private Models.Transfer ReadTransfer(IDataReader dr) {
        var count = new Models.Transfer {
            ID             = (int)dr["ID"],
            Date           = (DateTime)dr["Date"],
            Employee       = new UserInfo((int)dr["EmployeeID"], (string)dr["EmployeeName"]),
            Status         = (DocumentStatus)Convert.ToChar(dr["Status"]),
            StatusDate     = (DateTime)dr["StatusDate"],
            StatusEmployee = new UserInfo((int)dr["StatusEmployeeID"], (string)dr["StatusEmployeeName"]),
            WhsCode        = (string)dr["WhsCode"],
        };
        return count;
    }


    public AddItemResponse AddItem(AddItemParameter parameters, int employeeID) {
        throw new System.NotImplementedException();
    }

    public void UpdateLine(UpdateLineParameter parameters) {
        throw new System.NotImplementedException();
    }

    public bool CancelTransfer(int id, int employeeID) {
        throw new System.NotImplementedException();
    }

    public bool ProcessTransfer(int id, int employeeID, List<string> alertUsers) {
        throw new System.NotImplementedException();
    }

    public IEnumerable<TransferContent> GetTransferContent(int id, int binEntry) {
        throw new NotImplementedException();
    }
    public UpdateLineReturnValue ValidateUpdateLine(UpdateLineParameter updateLineParameter) {
        throw new NotImplementedException();
    }
    public static string GetQuery(string id) {
        string resourceName = $"Service.API.Transfer.Queries.{ConnectionController.DatabaseType}.{id}.sql";
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