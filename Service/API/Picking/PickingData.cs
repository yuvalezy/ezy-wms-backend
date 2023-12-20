using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Service.API.Picking.Models;
using Service.Shared.Company;
using Service.Shared.Data;

namespace Service.API.Picking;

public class PickingData {
    public PickingDocument GetPicking(int id, string whsCode, int? type, int? entry) {
        var pick = GetPickings(new PickingParameters { ID = id, WhsCode = whsCode, Statues = null }).FirstOrDefault();
        if (pick == null)
            return null;
        GetPickingDetail(pick, type, entry);
        return pick;
    }

    private void GetPickingDetail(PickingDocument pick, int? type, int? entry) {
        pick.Detail = new();
        Global.DataObject.ExecuteReader(GetQuery("GetPickingDetails"),
            new Parameters {
                new Parameter("@AbsEntry", SqlDbType.Int, pick.Entry),
                new Parameter("@Type", SqlDbType.Int, type.HasValue ? type.Value : DBNull.Value),
                new Parameter("@Entry", SqlDbType.Int, entry.HasValue ? entry.Value : DBNull.Value),
            },
            dr => pick.Detail.Add(PickingDocumentDetail.Read(dr)));
        if (!type.HasValue || !entry.HasValue || pick.Detail.Count == 0)
            return;
        GetPickingDetailItems(pick.Entry, pick.Detail[0]);
    }

    private void GetPickingDetailItems(int absEntry, PickingDocumentDetail detail) {
        detail.Items = new();
        Global.DataObject.ExecuteReader(GetQuery("GetPickingDetailItems"),
            new Parameters {
                new Parameter("@AbsEntry", SqlDbType.Int, absEntry),
                new Parameter("@Type", SqlDbType.Int, detail.Type),
                new Parameter("@Entry", SqlDbType.Int, detail.Entry),
            },
            dr => detail.Items.Add(PickingDocumentDetailItem.Read(dr)));
    }


    public IEnumerable<PickingDocument> GetPickings(PickingParameters parameters) {
        List<PickingDocument> values = new();
        var                   sb     = new StringBuilder(Environment.NewLine);
        if (parameters.Statues is { Length: > 0 }) {
            sb.AppendLine("and PICKS.\"Status\" in (");
            for (int i = 0; i < parameters.Statues.Length; i++) {
                if (i > 0)
                    sb.Append(", ");
                sb.Append($"'{(char)parameters.Statues[i]}'");
            }

            sb.Append(") ");
        }

        var queryParams = new Parameters {
            new Parameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = parameters.WhsCode }
        };

        if (parameters.ID != null) {
            queryParams.Add("@AbsEntry", SqlDbType.Int).Value = parameters.ID;
            sb.AppendLine(" and PICKS.\"AbsEntry\" = @AbsEntry ");
        }

        if (parameters.Date != null) {
            queryParams.Add("@Date", SqlDbType.DateTime).Value = parameters.Date;
            sb.AppendLine(" and DATEDIFF(day,PICKS.\"U_StatusDate\",@Date) = 0 ");
        }

        Global.DataObject.ExecuteReader(string.Format(GetQuery("GetPickings"), sb), queryParams, dr => values.Add(PickingDocument.Read(dr)));
        return values;
    }


    public static string GetQuery(string id) {
        string resourceName = $"Service.API.Picking.Queries.{ConnectionController.DatabaseType}.{id}.sql";
        var    assembly     = typeof(Queries).Assembly;
        string resourcePath = resourceName;

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null) {
            throw new ArgumentException($"Specified resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public AddItemResponse AddItem(int id, int pickEntry, int quantity, int empID) {
        AddItemResponse returnValue;
        try {
            Global.DataObject.BeginTransaction();
            Global.DataObject.Execute(GetQuery("AddItem"), new Parameters {
                new Parameter("@AbsEntry", SqlDbType.Int, id),
                new Parameter("@PickEntry", SqlDbType.Int, pickEntry),
                new Parameter("@Quantity", SqlDbType.Int, id, quantity),
                new Parameter("@empID", SqlDbType.Int, empID),
            });
            returnValue = AddItemResponse.OkResponse;
            Global.DataObject.CommitTransaction();
        }
        catch {
            Global.DataObject.RollbackTransaction();
            throw;
        }

        return returnValue;
    }

    public int ValidateAddItem(int id, int sourceType, int sourceEntry, string itemCode, int empID, int quantity, out int pickEntry) {
        int returnValue = -1, returnPickEntry = -1;
        Global.DataObject.ExecuteReader(GetQuery("ValidateAddItemParameters"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@SourceType", SqlDbType.Int, sourceType),
            new Parameter("@SourceEntry", SqlDbType.Int, sourceEntry),
            new Parameter("@ItemCode", SqlDbType.NVarChar, 50, itemCode),
            new Parameter("@empID", SqlDbType.Int, empID),
            new Parameter("@Quantity", SqlDbType.Int, quantity),
        }, dr => {
            returnPickEntry = (int)dr[0];
            returnValue     = (int)dr[1];
        });
        if (returnPickEntry == -1) {
            returnValue = -6;
        }
        pickEntry = returnPickEntry;
        return returnValue;
    }
}