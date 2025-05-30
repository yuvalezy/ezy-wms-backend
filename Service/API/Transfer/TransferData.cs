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
using Alert = Service.API.General.Alert;

namespace Service.API.Transfer;

public class TransferData {
    public int CreateTransfer(CreateParameters createParameters, int employeeID) {
        object name     = !string.IsNullOrWhiteSpace(createParameters.Name) ? createParameters.Name : DBNull.Value;
        object comments = !string.IsNullOrWhiteSpace(createParameters.Comments) ? createParameters.Comments : DBNull.Value;
        var @params = new Parameters {
            new Parameter("@Name", SqlDbType.NVarChar, 50, name),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@Comments", SqlDbType.NText, comments),
        };
        using var conn = Global.Connector;
        return conn.GetValue<int>(GetQuery("CreateTransfer"), @params);
    }

    public Models.Transfer ProcessInfo(int id) {
        var @params = new Parameters {
            new Parameter("@id", SqlDbType.Int, id),
        };
        using var conn     = Global.Connector;
        var       response = GetTransfer(id);
        conn.ExecuteReader(GetQuery("ProcessInfo"), @params, dr => response.IsComplete = !Convert.ToBoolean(dr["IsComplete"]) && Convert.ToBoolean(dr["HasItems"]));
        return response;
    }

    public Models.Transfer GetTransfer(int id) {
        Models.Transfer count = null;
        var             sb    = new StringBuilder(GetQuery("GetTransfers"));
        sb.Append(" where TRANSFERS.\"Code\" = @ID");
        using var conn = Global.Connector;
        conn.ExecuteReader(sb, new Parameter("@ID", SqlDbType.Int, id), dr => count = ReadTransfer(dr));
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
         TRANSFERS.""Name"",
         TRANSFERS.""U_Date"",
         TRANSFERS.""U_empID"",
         T1.""firstName"",
         T1.""lastName"",
         TRANSFERS.""U_Status"",
         TRANSFERS.""U_StatusDate"",
         TRANSFERS.""U_StatusEmpID"",
         T2.""firstName"",
         T2.""lastName"",
         TRANSFERS.""U_WhsCode"",
         Cast(TRANSFERS.""U_Comments"" as varchar(8000)) ");
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

        using var conn = Global.Connector;
        conn.ExecuteReader(query, queryParams, dr => {
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
            Name           = dr["Name"].ToString(),
            Date           = (DateTime)dr["Date"],
            Employee       = new UserInfo((int)dr["EmployeeID"], (string)dr["EmployeeName"]),
            Status         = (DocumentStatus)Convert.ToChar(dr["Status"]),
            StatusDate     = (DateTime)dr["StatusDate"],
            StatusEmployee = new UserInfo((int)dr["StatusEmployeeID"], (string)dr["StatusEmployeeName"]),
            WhsCode        = (string)dr["WhsCode"],
            Comments       = dr["Comments"].ToString(),
        };
        return count;
    }


    public int ValidateAddItem(DataConnector conn, AddItemParameter parameters, int employeeID) =>
        conn.GetValue<int>(GetQuery("ValidateAddItemParameters"), [
            new Parameter("@ID", SqlDbType.Int, parameters.ID),
            new Parameter("@ItemCode", SqlDbType.NVarChar, 50, parameters.ItemCode),
            new Parameter("@BarCode", SqlDbType.NVarChar, 254, parameters.BarCode),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@BinEntry", SqlDbType.Int, parameters.BinEntry is > 0 ? parameters.BinEntry.Value : DBNull.Value),
            new Parameter("@Quantity", SqlDbType.Int, parameters.Quantity),
            new Parameter("@Type", SqlDbType.Char, 1, ((char)parameters.Type).ToString()),
            new Parameter("@Unit", SqlDbType.SmallInt, 1, parameters.Unit)
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
            new Parameter("@Type", SqlDbType.Char, 1, ((char)parameters.Type).ToString()),
            new Parameter("@Unit", SqlDbType.SmallInt, 1, parameters.Unit)
        ], dr => returnValue.LineID = (int)dr["LineID"]);
        return returnValue;
    }

    public void UpdateLine(DataConnector conn, UpdateLineParameter updateLineParameter) {
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

        // if (updateLineParameter.Quantity.HasValue) {
        //     if (comma)
        //         sb.AppendLine(", ");
        //     sb.AppendLine("\"U_Quantity\" = @Quantity ");
        //     parameters.Add(new Parameter("@Quantity", SqlDbType.Int) { Value = updateLineParameter.Quantity.Value });
        // }

        sb.AppendLine("where U_ID = @ID and \"U_LineID\" = @LineID");

        conn.Execute(sb.ToString(), parameters);
    }

    public void UpdateLineQuantity(DataConnector conn, UpdateLineParameter updateLineParameter) {
        var parameters = new Parameters {
            new Parameter("@ID", SqlDbType.Int) { Value       = updateLineParameter.ID },
            new Parameter("@LineID", SqlDbType.Int) { Value   = updateLineParameter.LineID },
            new Parameter("@Quantity", SqlDbType.Int) { Value = updateLineParameter.Quantity.Value }
        };
        conn.Execute(GetQuery("UpdateSourceLineQuantity"), parameters);
    }

    public bool CancelTransfer(int id, int employeeID) {
        var transfer = GetTransfer(id);
        if (transfer.Status is not (DocumentStatus.Open or DocumentStatus.InProgress))
            throw new Exception("Cannot cancel transfer if the Status is not Open or In Progress");
        UpdateTransferStatus(id, employeeID, DocumentStatus.Cancelled);
        return true;
    }

    public bool ProcessTransfer(int id, int employeeID, List<string> sendTo) {
        var transfer = GetTransfer(id);
        if (transfer.Status != DocumentStatus.InProgress)
            throw new Exception("Cannot process transfer if the Status is not In Progress");
        UpdateTransferStatus(id, employeeID, DocumentStatus.Processing);
        try {
            using var creation = new TransferCreation(id, employeeID);
            creation.Execute();
            UpdateTransferStatus(id, employeeID, DocumentStatus.Finished);
            creation.SetFinishedLines();
            ProcessTransferSendAlert(id, sendTo, creation);
            return true;
        }
        catch (Exception e) {
            UpdateTransferStatus(id, employeeID, DocumentStatus.InProgress);
            throw;
        }
    }

    private void ProcessTransferSendAlert(int id, List<string> sendTo, TransferCreation creation) {
        try {
            using var alert = new Alert();
            alert.Subject = string.Format(ErrorMessages.WMSTransactionAlert, id);
            var transactionColumn = new AlertColumn(ErrorMessages.WMSTransaction);
            var transferColumn    = new AlertColumn(ErrorMessages.InventoryTransfer, true);
            alert.Columns.AddRange([transactionColumn, transferColumn]);
            transactionColumn.Values.Add(new AlertValue(id.ToString()));
            transferColumn.Values.Add(new AlertValue(creation.Number.ToString(), "67", creation.Entry.ToString()));

            alert.Send(sendTo);
        }
        catch (Exception e) {
            //todo log error handler
        }
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
                queryParams.Add(new Parameter("@ItemCode", SqlDbType.NVarChar, !contentParameters.TargetBinQuantity && contentParameters.ItemCode != null ? contentParameters.ItemCode : DBNull.Value));
                queryParams.Add(new Parameter("@BinEntry", SqlDbType.Int, contentParameters.TargetBinQuantity ? contentParameters.BinEntry : DBNull.Value));
                break;
        }

        Dictionary<string, TransferContent> control = new();

        using var conn = Global.Connector;
        conn.ExecuteReader(GetQuery(query), queryParams, dr => {
            var content = new TransferContent {
                Code       = (string)dr["ItemCode"],
                Name       = dr["ItemName"].ToString(),
                Quantity   = Convert.ToInt32(dr["Quantity"]),
                NumInBuy   = Convert.ToInt32(dr["NumInBuy"]),
                BuyUnitMsr = dr["BuyUnitMsr"].ToString(),
                PurPackUn  = Convert.ToInt32(dr["PurPackUn"]),
                PurPackMsr = dr["PurPackMsr"].ToString()
            };
            switch (contentParameters.Type) {
                case SourceTarget.Source:
                    content.Unit       = (UnitType)Convert.ToInt16(dr["Unit"]);
                    break;
                case SourceTarget.Target: {
                    content.Progress     = Convert.ToInt32(dr["Progress"]);
                    content.OpenQuantity = Convert.ToInt32(dr["OpenQuantity"]);
                    if (contentParameters.TargetBinQuantity) {
                        content.BinQuantity = Convert.ToInt32(dr["BinQuantity"]);
                    }

                    break;
                }
            }

            list.Add(content);
            if (contentParameters.Type == SourceTarget.Target && contentParameters.TargetBins)
                control.Add(content.Code, content);
        });

        if (contentParameters.Type == SourceTarget.Target && contentParameters.TargetBins)
            GetTransferContentBins(queryParams, control);

        return list;
    }

    private static void GetTransferContentBins(Parameters queryParams, Dictionary<string, TransferContent> control) {
        using var conn = Global.Connector;
        conn.ExecuteReader(GetQuery("TransferContentTargetBins"), queryParams, dr => {
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
        var       data = new List<TransferContentTargetItemDetail>();
        using var conn = Global.Connector;
        conn.ExecuteReader(GetQuery("TransferContentTargetItemDetail"), [
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

    public int ValidateUpdateLine(DataConnector conn, UpdateLineParameter parameters) {
        return conn.GetValue<int>(GetQuery("ValidateUpdateLineParameters"), [
            new Parameter("@ID", SqlDbType.Int, parameters.ID),
            new Parameter("@LineID", SqlDbType.Int, parameters.LineID),
            new Parameter("@Reason", SqlDbType.Int, parameters.CloseReason.HasValue ? parameters.CloseReason.Value : DBNull.Value),
            new Parameter("@Quantity", SqlDbType.Int, parameters.Quantity.HasValue ? parameters.Quantity.Value : DBNull.Value),
        ]);
    }

    public void UpdateContentTargetDetail(UpdateDetailParameters parameters) {
        using var conn = Global.Connector;
        if (parameters.QuantityChanges != null) {
            try {
                var control = new List<UpdateLineParameter>();
                foreach (var pair in parameters.QuantityChanges) {
                    var updateLineParameter = new UpdateLineParameter {
                        ID       = parameters.ID,
                        LineID   = pair.Key,
                        Quantity = pair.Value
                    };
                    var isValid = (UpdateLineReturnValue)ValidateUpdateLine(conn, updateLineParameter);
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

                control.ForEach(updateLineParameter => UpdateLine(conn, updateLineParameter));
            }
            catch (Exception e) {
                conn.RollbackTransaction();
                throw new Exception($"Update Quantity Error: {e.Message}");
            }
        }

        try {
            parameters.RemoveRows?.ForEach(row => {
                UpdateLine(conn, new UpdateLineParameter {
                    ID            = parameters.ID,
                    LineID        = row,
                    InternalClose = true,
                });
            });
        }
        catch (Exception e) {
            conn.RollbackTransaction();
            throw new Exception("Remove Rows Error: " + e.Message);
        }

        conn.CommitTransaction();
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
        using var conn = Global.Connector;
        conn.Execute(GetQuery("UpdateTransferStatus"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status)
        ]);
        conn.Execute(GetQuery("UpdateTransferLineStatus"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status)
        ]);
    }

    public int CreateTransferRequest(TransferContent[] contents, EmployeeData employee) {
        using var creation = new TransferRequestCreation(contents, employee);
        creation.Execute();
        return creation.Number;
    }
}