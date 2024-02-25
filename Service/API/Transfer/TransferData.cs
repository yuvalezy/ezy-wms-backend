using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using CrystalDecisions.ReportAppServer.DataDefModel;
using Service.API.General;
using Service.API.General.Models;
using Service.API.Models;
using Service.API.Transfer.Models;
using Service.Shared;
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

    public bool IsComplete(int id) {
        var @params = new Parameters {
            new Parameter("@id", SqlDbType.Int, id),
        };
        return !Global.DataObject.GetValue<bool>(GetQuery("CheckIsCompleted"), @params);
    }

    public Models.Transfer GetTransfer(int id) {
        Models.Transfer count = null;
        var             sb    = new StringBuilder(GetQuery("GetTransfers"));
        sb.Append(" where TRANSFERS.\"Code\" = @ID");
        Global.DataObject.ExecuteReader(sb, new Parameter("@ID", SqlDbType.Int, id), dr => count = ReadTransfer(dr));
        return count;
    }

    public IEnumerable<Models.Transfer> GetTransfers(FilterParameters parameters) {
        List<Models.Transfer> counts            = [];
        var                   sb                = new StringBuilder(GetQuery("GetTransfers"));
        string                additionalColumns = string.Empty;
        var queryParams = new Parameters {
            new Parameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = parameters.WhsCode }
        };

        if (parameters.Progress) {
            additionalColumns = $@"{Environment.NewLine}, IIF(Sum(IIF(TRANS1.""U_Type"" = 'S', TRANS1.""U_Quantity"", 0)) > 0,
                    Sum(IIF(TRANS1.""U_Type"" = 'T', TRANS1.""U_Quantity"", 0)) * 100 / Sum(IIF(TRANS1.""U_Type"" = 'S', TRANS1.""U_Quantity"", 0)), 0) ""Progress"" ";
            sb.Append("left outer join \"@LW_YUVAL08_TRANS1\" TRANS1 on TRANS1.\"U_ID\" = TRANSFERS.\"Code\" and TRANS1.\"U_LineStatus\" <> 'C' ");
        }

        sb.Append($" where TRANSFERS.\"U_WhsCode\" = @WhsCode ");
        if (parameters.Status is { Length: > 0 }) {
            sb.Append(" and TRANSFERS.\"U_Status\" in ('");
            sb.Append(string.Join("','", parameters.Status.Select(v => (char)v)));
            sb.Append("') ");
        }

        if (parameters.ID != null) {
            queryParams.Add("@Code", SqlDbType.Int).Value = parameters.ID;
            sb.Append(" and TRANSFERS.\"Code\" = @Code ");
        }

        if (parameters.Date != null) {
            queryParams.Add("@Date", SqlDbType.DateTime).Value = parameters.Date;
            sb.Append(" and DATEDIFF(day,TRANSFERS.\"U_StatusDate\",@Date) = 0 ");
        }

        if (parameters.Progress) {
            sb.Append(@"Group By TRANSFERS.""Code"",
         TRANSFERS.""U_Date"",
         TRANSFERS.""U_empID"",
         T1.""firstName"",
         T1.""lastName"",
         TRANSFERS.""U_Status"",
         TRANSFERS.""U_StatusDate"",
         TRANSFERS.""U_StatusEmpID"",
         T2.""firstName"",
         T2.""lastName"",
         TRANSFERS.""U_WhsCode"" ");
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
        query = string.Format(query, additionalColumns);

        Global.DataObject.ExecuteReader(query, queryParams, dr => {
            var transfer = ReadTransfer(dr);
            if (parameters.Progress) {
                transfer.Progress = Convert.ToInt32(dr["Progress"]);
            }

            counts.Add(transfer);
        });
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


    public int ValidateAddItem(AddItemParameter parameters, int employeeID) =>
        Global.DataObject.GetValue<int>(GetQuery("ValidateAddItemParameters"), [
            new Parameter("@ID", SqlDbType.Int, parameters.ID),
            new Parameter("@ItemCode", SqlDbType.NVarChar, 50, parameters.ItemCode),
            new Parameter("@BarCode", SqlDbType.NVarChar, 254, parameters.BarCode),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@BinEntry", SqlDbType.Int, parameters.BinEntry is > 0 ? parameters.BinEntry.Value : DBNull.Value),
            new Parameter("@Quantity", SqlDbType.Int, parameters.Quantity),
            new Parameter("@Type", SqlDbType.Char, 1, ((char)parameters.Type).ToString())
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
                new Parameter("@Quantity", SqlDbType.Int, parameters.Quantity),
                new Parameter("@Type", SqlDbType.Char, 1, ((char)parameters.Type).ToString())
            ], dr => returnValue.LineID = (int)dr["LineID"]);
            Global.DataObject.CommitTransaction();
            return returnValue;
        }
        catch {
            Global.DataObject.RollbackTransaction();
            throw;
        }
    }

    public void UpdateLine(UpdateLineParameter updateLineParameter) {
        var parameters = new Parameters {
            new Parameter("@ID", SqlDbType.Int) { Value     = updateLineParameter.ID },
            new Parameter("@LineID", SqlDbType.Int) { Value = updateLineParameter.LineID },
        };
        var  sb    = new StringBuilder("update \"@LW_YUVAL08_TRANS1\" set ");
        bool comma = false;
        if (updateLineParameter.Comment != null) {
            sb.AppendLine("\"U_Comments\" = @Comments ");
            parameters.Add("@Comments", SqlDbType.NText).Value = updateLineParameter.Comment;
            comma                                              = true;
        }

        if (updateLineParameter.CloseReason.HasValue || updateLineParameter.InternalClose) {
            if (comma)
                sb.AppendLine(", ");
            sb.AppendLine("\"U_LineStatus\" = 'C' ");
            comma = true;
        }

        if (updateLineParameter.CloseReason.HasValue) {
            if (comma)
                sb.AppendLine(", ");
            sb.AppendLine("\"U_StatusReason\" = @Reason ");
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

    public bool CancelTransfer(int id, int employeeID) {
        var transfer = GetTransfer(id);
        if (transfer.Status is not (DocumentStatus.Open or DocumentStatus.InProgress))
            throw new Exception("Cannot cancel transfer if the Status is not Open or In Progress");
        UpdateTransferStatus(id, employeeID, DocumentStatus.Cancelled);
        return true;
    }

    public bool ProcessTransfer(int id, int employeeID, List<string> alertUsers) {
        throw new System.NotImplementedException();
    }

    public IEnumerable<TransferContent> GetTransferContent(TransferContentParameters contentParameters) {
        var        list        = new List<TransferContent>();
        string     query       = $"TransferContent{contentParameters.Type.ToString()}";
        Parameters queryParams = [new Parameter("@ID", SqlDbType.Int, contentParameters.ID)];
        switch (contentParameters.Type) {
            case SourceTarget.Source:
                queryParams.Add(new Parameter("@BinEntry", SqlDbType.Int, contentParameters.BinEntry > 0 ? contentParameters.BinEntry : DBNull.Value));
                break;
            case SourceTarget.Target:
                queryParams.Add(new Parameter("@ItemCode", SqlDbType.NVarChar, contentParameters.ItemCode != null ? contentParameters.ItemCode : DBNull.Value));
                break;
        }

        Dictionary<string, TransferContent> control = new();

        Global.DataObject.ExecuteReader(GetQuery(query), queryParams, dr => {
            var content = new TransferContent {
                Code     = (string)dr["ItemCode"],
                Name     = dr["ItemName"].ToString(),
                Quantity = Convert.ToInt32(dr["Quantity"])
            };
            if (contentParameters.Type == SourceTarget.Target) {
                content.Progress     = Convert.ToInt32(dr["Progress"]);
                content.OpenQuantity = Convert.ToInt32(dr["OpenQuantity"]);
            }

            list.Add(content);
            control.Add(content.Code, content);
        });

        if (contentParameters.Type == SourceTarget.Target && contentParameters.TargetBins)
            GetTransferContentBins(queryParams, control);

        return list;
    }

    private static void GetTransferContentBins(Parameters queryParams, Dictionary<string, TransferContent> control) {
        Global.DataObject.ExecuteReader(GetQuery("TransferContentTargetBins"), queryParams, dr => {
            string itemCode = (string)dr["ItemCode"];
            var bin = new TransferContentBin {
                Entry    = (int)dr["Entry"],
                Code     = (string)dr["Code"],
                Quantity = Convert.ToInt32(dr["Quantity"])
            };
            control[itemCode].Bins ??= [];
            control[itemCode].Bins.Add(bin);
        });
    }

    public IEnumerable<TransferContentTargetItemDetail> TransferContentTargetDetail(TransferContentTargetItemDetailParameters queryParams) {
        var data = new List<TransferContentTargetItemDetail>();
        Global.DataObject.ExecuteReader(GetQuery("TransferContentTargetItemDetail"), [
                new Parameter("@ID", SqlDbType.Int) { Value                = queryParams.ID },
                new Parameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = queryParams.ItemCode },
                new Parameter("@BinEntry", SqlDbType.Int) { Value          = queryParams.BinEntry }
            ],
            dr => {
                int    lineID       = (int)dr["LineID"];
                string employeeName = (string)dr["EmployeeName"];
                var    timestamp    = (DateTime)dr["TimeStamp"];
                int    quantity     = Convert.ToInt32(dr["Quantity"]);

                var line = new TransferContentTargetItemDetail {
                    LineID       = lineID,
                    EmployeeName = employeeName,
                    TimeStamp    = timestamp,
                    Quantity     = quantity,
                };
                data.Add(line);
            });
        return data;
    }

    public int ValidateUpdateLine(UpdateLineParameter parameters) {
        return Global.DataObject.GetValue<int>(GetQuery("ValidateUpdateLineParameters"), [
            new Parameter("@ID", SqlDbType.Int, parameters.ID),
            new Parameter("@LineID", SqlDbType.Int, parameters.LineID),
            new Parameter("@Reason", SqlDbType.Int, parameters.CloseReason.HasValue ? parameters.CloseReason.Value : DBNull.Value),
            new Parameter("@Quantity", SqlDbType.Int, parameters.Quantity.HasValue ? parameters.Quantity.Value : DBNull.Value),
        ]);
    }

    public void UpdateContentTargetDetail(UpdateDetailParameters parameters) {
        if (parameters.QuantityChanges != null) {
            try {
                var control = new List<UpdateLineParameter>();
                foreach (var pair in parameters.QuantityChanges) {
                    var updateLineParameter = new UpdateLineParameter {
                        ID       = parameters.ID,
                        LineID   = pair.Key,
                        Quantity = pair.Value
                    };
                    var isValid = (UpdateLineReturnValue)ValidateUpdateLine(updateLineParameter);
                    switch (isValid) {
                        case UpdateLineReturnValue.Status:
                            throw new Exception($"Transfer status is not In Progress");
                        case UpdateLineReturnValue.LineStatus:
                            throw new Exception($"Trans Line Status is not In Progress");
                        case UpdateLineReturnValue.QuantityMoreThenAvailable:
                            throw new Exception(ErrorMessages.QuantityMoreThenAvailableCurrent);
                    }

                    control.Add(updateLineParameter);
                }

                control.ForEach(UpdateLine);
            }
            catch (Exception e) {
                throw new Exception($"Update Quantity Error: {e.Message}");
            }
        }

        try {
            parameters.RemoveRows?.ForEach(row => {
                UpdateLine(new UpdateLineParameter {
                    ID            = parameters.ID,
                    LineID        = row,
                    InternalClose = true,
                });
            });
        }
        catch (Exception e) {
            throw new Exception("Remove Rows Error: " + e.Message);
        }
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
    private static void UpdateTransferStatus(int id, int employeeID, DocumentStatus status) {
        Global.DataObject.Execute(GetQuery("UpdateTransferStatus"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status)
        ]);
        Global.DataObject.Execute(GetQuery("UpdateTransferLineStatus"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status)
        ]);
    }
}