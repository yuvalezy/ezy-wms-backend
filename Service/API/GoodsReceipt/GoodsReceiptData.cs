using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Service.API.General;
using Service.API.General.Models;
using Service.API.GoodsReceipt.Models;
using Service.API.Models;
using Service.API.Transfer.Models;
using Service.Shared.Company;
using Service.Shared.Data;
using Service.Shared.Utils;
using AddItemResponse = Service.API.GoodsReceipt.Models.AddItemResponse;
using Alert = Service.API.General.Alert;
using CreateParameters = Service.API.GoodsReceipt.Models.CreateParameters;
using FilterParameters = Service.API.GoodsReceipt.Models.FilterParameters;
using OrderBy = Service.API.GoodsReceipt.Models.OrderBy;
using UpdateLineParameter = Service.API.GoodsReceipt.Models.UpdateLineParameter;

namespace Service.API.GoodsReceipt;

public class GoodsReceiptData {
    public bool CancelDocument(int id, int employeeID) {
        var doc = GetDocument(id);
        if (doc.Status is not (DocumentStatus.Open or DocumentStatus.InProgress))
            throw new Exception("Cannot cancel document if the Status is not Open or In Progress");
        UpdateDocumentStatus(id, employeeID, DocumentStatus.Cancelled);
        return true;
    }

    public bool ProcessDocument(int id, int employeeID, bool enableBin, List<string> sendTo) {
        var doc = GetDocument(id);
        if (doc.Status != DocumentStatus.InProgress)
            throw new Exception("Cannot process document if the Status is not In Progress");
        UpdateDocumentStatus(id, employeeID, DocumentStatus.Processing);
        try {
            using var creation = new GoodsReceiptCreation(id, employeeID, enableBin);
            creation.Execute();
            UpdateDocumentStatus(id, employeeID, DocumentStatus.Finished);
            creation.SetFinishedLines();
            ProcessDocumentSendAlert(id, sendTo, creation);
            return true;
        }
        catch (Exception e) {
            UpdateDocumentStatus(id, employeeID, DocumentStatus.InProgress);
            throw;
        }
    }

    private static void ProcessDocumentSendAlert(int id, List<string> sendTo, GoodsReceiptCreation creation) {
        try {
            using var alert = new Alert();
            alert.Subject = string.Format(ErrorMessages.WMSTransactionAlert, id);
            var transactionColumn = new AlertColumn(ErrorMessages.WMSTransaction);
            var documentColumn    = new AlertColumn(Global.GRPODraft ? ErrorMessages.Draft : ErrorMessages.PurchaseDeliveryNote, true);
            alert.Columns.AddRange(new[] { transactionColumn, documentColumn });
            creation.NewEntries.ForEach(tuple => {
                transactionColumn.Values.Add(new AlertValue(tuple.Entry.ToString()));
                documentColumn.Values.Add(new AlertValue(tuple.Number.ToString(), Global.GRPODraft ? "112" : "20", tuple.Entry.ToString()));
            });

            alert.Send(sendTo);
        }
        catch (Exception e) {
            //todo log error handler
        }
    }

    public int CreateDocument(CreateParameters createParameters, int employeeID) {
        var @params = new Parameters {
            new Parameter("@Name", SqlDbType.NVarChar, 50, createParameters.Name),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@CardCode", SqlDbType.NVarChar, 50, DBNull.Value),
            // ReSharper disable once PossibleInvalidOperationException
            new Parameter("@Type", SqlDbType.Char, 1, ((char)createParameters.Type).ToString()),
        };
        if (!string.IsNullOrWhiteSpace(createParameters.CardCode))
            @params["@CardCode"].Value = createParameters.CardCode;
        using var conn = Global.Connector;
        int       id   = conn.GetValue<int>(GetQuery("CreateGoodsReceipt"), @params);

        if (createParameters.Type != GoodsReceiptType.SpecificOrders)
            return id;

        string query = GetQuery("CreateGoodsReceiptDocument");
        @params = [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@ObjType", SqlDbType.Int),
            new Parameter("@DocEntry", SqlDbType.Int)
        ];
        createParameters.Documents.ForEach(value => {
            @params["@ObjType"].Value  = value.ObjectType;
            @params["@DocEntry"].Value = value.DocumentEntry;
            conn.Execute(query, @params);
        });

        return id;
    }

    public int ValidateUpdateLine(DataConnector conn, int id, int lineID, int? closeReason = null) =>
        conn.GetValue<int>(GetQuery("ValidateUpdateLineParameters"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@LineID", SqlDbType.Int, lineID),
            new Parameter("@Reason", SqlDbType.Int, closeReason.HasValue ? closeReason.Value : DBNull.Value)
        ]);

    public void UpdateLine(DataConnector conn, UpdateLineParameter updateLineParameter, int empID) {
        int id     = updateLineParameter.ID;
        int lineID = updateLineParameter.LineID;
        var parameters = new Parameters {
            new Parameter("@ID", SqlDbType.Int) { Value     = id },
            new Parameter("@LineID", SqlDbType.Int) { Value = lineID },
        };
        var  sb       = new StringBuilder("update \"@LW_YUVAL08_GRPO1\" set ");
        bool comma    = false;
        bool userSign = false;
        if (updateLineParameter.Comment != null) {
            sb.AppendLine("\"U_Comments\" = @Comments ");
            parameters.Add("@Comments", SqlDbType.NText).Value = updateLineParameter.Comment;
            comma                                              = true;
        }

        if (updateLineParameter.CloseReason.HasValue || updateLineParameter.InternalClose) {
            if (comma)
                sb.AppendLine(", ");
            sb.AppendLine("\"U_LineStatus\" = 'C' ");
            comma    = true;
            userSign = true;
        }

        if (updateLineParameter.CloseReason.HasValue) {
            if (comma)
                sb.AppendLine(", ");
            sb.AppendLine("\"U_StatusReason\" = @Reason ");
            parameters.Add(new Parameter("@Reason", SqlDbType.Int) { Value = updateLineParameter.CloseReason.Value });
            userSign = true;
        }

        if (userSign) {
            sb.AppendLine(", \"U_StatusUserSign\" = @UserSign, \"U_StatusTimeStamp\" = getdate() ");
            parameters.Add(new Parameter("@UserSign", SqlDbType.Int) { Value = empID });
        }

        sb.AppendLine("where U_ID = @ID and \"U_LineID\" = @LineID");

        conn.Execute(sb.ToString(), parameters);

        if (updateLineParameter.CloseReason.HasValue || updateLineParameter.InternalClose) {
            conn.Execute("update \"@LW_YUVAL08_GRPO2\" set \"U_TargetStatus\" = 'C' where U_ID = @ID and U_LineID = @LineID", [
                new Parameter("@ID", SqlDbType.Int) { Value     = id },
                new Parameter("@LineID", SqlDbType.Int) { Value = lineID }
            ]);
        }
    }

    public UpdateItemResponse UpdateLineQuantity(DataConnector conn, UpdateLineQuantityParameter updateLineParameter, int empID) {
        UpdateItemResponse returnValue = null;
        int                id          = updateLineParameter.ID;
        int                lineID      = updateLineParameter.LineID;
        conn.ExecuteReader(GetQuery("UpdateSourceLineQuantity"), [
            new Parameter("@ID", SqlDbType.Int) { Value       = id },
            new Parameter("@LineID", SqlDbType.Int) { Value   = lineID },
            new Parameter("@UserSign", SqlDbType.Int) { Value = empID },
            new Parameter("@Quantity", SqlDbType.Int) { Value = updateLineParameter.Quantity },
        ], dr => {
            returnValue = new UpdateItemResponse {
                Fulfillment = (int)dr["Fulfillment"] > 0,
                Showroom    = (int)dr["Showroom"] > 0,
                Warehouse   = (int)dr["Warehouse"] > 0,
                Quantity    = (int)dr["PurPackUn"]
            };
        });
        if (returnValue == null)
            throw new Exception("Update Item Result Empty!");

        return returnValue;
    }

    public int ValidateAddItem(DataConnector conn, int id, string itemCode, string barCode, int empID) =>
        conn.GetValue<int>(GetQuery("ValidateAddItemParameters"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@ItemCode", SqlDbType.NVarChar, 50, itemCode),
            new Parameter("@BarCode", SqlDbType.NVarChar, 254, barCode),
            new Parameter("@empID", SqlDbType.Int, empID)
        ]);

    public AddItemResponse AddItem(DataConnector conn, int id, string itemCode, string barcode, int employeeID, UnitType unit) {
        AddItemResponse returnValue = null;
        try {
            conn.ExecuteReader(GetQuery("AddItem"), [
                new Parameter("@ID", SqlDbType.Int, id),
                new Parameter("@ItemCode", SqlDbType.NVarChar, 50, itemCode),
                new Parameter("@BarCode", SqlDbType.NVarChar, 254, barcode),
                new Parameter("@empID", SqlDbType.Int, employeeID),
                new Parameter("@Unit", SqlDbType.SmallInt, unit)
            ], dr => {
                returnValue = new AddItemResponse {
                    LineID      = (int)dr["LineID"],
                    Fulfillment = (int)dr["Fulfillment"] > 0,
                    Showroom    = (int)dr["Showroom"] > 0,
                    Warehouse   = (int)dr["Warehouse"] > 0,
                    Quantity    = 1,
                    NumInBuy    = (int)dr["NumInBuy"],
                    BuyUnitMsr  = dr["BuyUnitMsr"].ToString(),
                    PurPackUn   = (int)dr["PurPackUn"],
                    PurPackMsr  = dr["PurPackMsr"].ToString()
                };
            });
            if (returnValue == null)
                throw new Exception("Add Item Result Empty!");
        }
        catch (Exception ex) {
            Console.WriteLine("Add Item Error GRPO: " + ex.Message);
            if (!ex.Message.Contains("No valid source found for item"))
                throw;
            returnValue = new AddItemResponse {
                ErrorMessage = string.Format(ErrorMessages.GoodsReceiptData_AddItem_No_valid_source_purchase_document_found_for_item__0_, itemCode)
            };
        }

        return returnValue;
    }

    public Document GetDocument(int id) {
        Document doc   = null;
        string   query = GetQuery("GetGoodsReceipts");
        query = query.Replace("{top}", "");
        var sb = new StringBuilder(query);
        sb.Append(" where DOCS.\"Code\" = @ID");
        using var conn = Global.Connector;
        conn.ExecuteReader(sb, new Parameter("@ID", SqlDbType.Int, id), dr => doc = ReadDocument(dr));
        GetDocumentsSpecificDocuments(doc);
        return doc;
    }

    public IEnumerable<Document> GetDocuments(FilterParameters parameters) {
        List<Document> docs = [];
        var            sb   = new StringBuilder(GetQuery("GetGoodsReceipts"));
        var queryParams = new Parameters {
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

        if (parameters.LastID.HasValue && parameters.LastID != -1) {
            queryParams.Add("@LastID", SqlDbType.Int).Value = parameters.LastID.Value;
            sb.Append(" and DOCS.\"Code\" < @LastID ");
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

        if (parameters.DateFrom != null) {
            queryParams.Add("@DateFrom", SqlDbType.DateTime).Value = parameters.DateFrom;
            sb.Append(" and DATEDIFF(day,DOCS.\"U_StatusDate\",@DateFrom) <= 0 ");
        }

        if (parameters.DateTo != null) {
            queryParams.Add("@DateTo", SqlDbType.DateTime).Value = parameters.DateTo;
            sb.Append(" and DATEDIFF(day,DOCS.\"U_StatusDate\",@DateTo) >= 0 ");
        }

        if (parameters.GRPO != null) {
            queryParams.Add("@GRPO", SqlDbType.Int).Value = parameters.GRPO;
            sb.Append(" and OPDN.\"DocNum\" = @GRPO ");
        }

        if (parameters.PurchaseOrder != null) {
            queryParams.Add("@PurchaseOrder", SqlDbType.Int).Value = parameters.PurchaseOrder;
            sb.Append(
                " and DOCS.Code in (select T0.U_ID from \"@LW_YUVAL08_GRPO4\" T0 inner join OPOR T1 on T1.\"DocEntry\" = T0.\"U_SourceEntry\" where T0.\"U_SourceType\" = 22 and T1.\"DocNum\" = @PurchaseOrder) ");
        }

        if (parameters.ReservedInvoice != null) {
            queryParams.Add("@ReservedInvoice", SqlDbType.Int).Value = parameters.ReservedInvoice;
            sb.Append(
                " and DOCS.Code in (select T0.U_ID from \"@LW_YUVAL08_GRPO4\" T0 inner join OPCH T1 on T1.\"DocEntry\" = T0.\"U_SourceEntry\" where T0.\"U_SourceType\" = 18 and T1.\"DocNum\" = @ReservedInvoice) ");
        }

        sb.Append(" order by DOCS.");
        switch (parameters.OrderBy ?? OrderBy.ID) {
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

        if (parameters.OrderByDesc ?? true)
            sb.Append(" desc");

        if (parameters.PageSize.HasValue) {
            if (parameters.PageNumber.HasValue) {
                int pageSize   = parameters.PageSize.Value;
                int pageNumber = parameters.PageNumber.Value;
                int rowOffset  = (pageNumber - 1) & pageSize;
                sb.Append($" OFFSET {rowOffset} ROWS FETCH NEXT {pageSize} ROWS ONLY");
            }
        }

        string query = sb.ToString();
        if (!parameters.PageSize.HasValue || !parameters.LastID.HasValue) {
            query = query.Replace("{top}", "");
        }

        if (parameters.LastID.HasValue) {
            query = query.Replace("{top}", $" top {parameters.PageSize} ");
        }

        using var conn = Global.Connector;
        conn.ExecuteReader(query, queryParams, dr => docs.Add(ReadDocument(dr)));
        var documents = docs.ToArray();
        GetDocumentsSpecificDocuments(documents);
        return documents;
    }

    private static Document ReadDocument(IDataRecord dr) {
        var doc = new Document {
            ID             = (int)dr["ID"],
            Name           = (string)dr["Name"],
            Date           = (DateTime)dr["Date"],
            Employee       = new UserInfo((int)dr["EmployeeID"], (string)dr["EmployeeName"]),
            Status         = (DocumentStatus)Convert.ToChar(dr["Status"]),
            StatusDate     = (DateTime)dr["StatusDate"],
            StatusEmployee = new UserInfo((int)dr["StatusEmployeeID"], (string)dr["StatusEmployeeName"]),
            WhsCode        = (string)dr["WhsCode"],
            Type           = (GoodsReceiptType)Convert.ToChar(dr["Type"])
        };
        if (dr["CardCode"] != DBNull.Value)
            doc.BusinessPartner = new BusinessPartner((string)dr["CardCode"], dr["CardName"].ToString());
        return doc;
    }

    private void GetDocumentsSpecificDocuments(params Document[] documents) {
        var filtered = documents.Where(v => v.Type == GoodsReceiptType.SpecificOrders).ToArray();
        if (filtered.Length == 0)
            return;
        string query = filtered.Aggregate("", (a, b) => a + a.AggregateQuery() + b.ID);
        query = $"""
                 select X0."U_ID" ID, X0."U_ObjType" "ObjType", X0."U_DocEntry" "DocEntry", COALESCE(X1."DocNum", X2."DocNum") "DocNum"
                 from "@LW_YUVAL08_GRPO3" X0
                 left outer join OPOR X1 on X1."DocEntry" = X0."U_DocEntry" and X1."ObjType" = X0."U_ObjType"
                 left outer join OPCH X2 on X2."DocEntry" = X0."U_DocEntry" and X2."ObjType" = X0."U_ObjType"
                 where X0."U_ID" in ({query})
                 """;
        using var conn = Global.Connector;
        using var dt   = conn.GetDataTable(query);
        using var dv   = new DataView(dt);
        foreach (var document in filtered) {
            dv.RowFilter = $"ID = {document.ID}";
            var specific = new List<DocumentParameter>();
            document.SpecificDocuments = specific;
            specific.AddRange(
                from DataRowView dvr
                    in dv
                select new DocumentParameter {
                    ObjectType     = (int)dvr["ObjType"],
                    DocumentNumber = Convert.ToInt32(dvr["DocNum"]),
                });
        }
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
        using var conn = Global.Connector;
        conn.Execute(GetQuery("UpdateGoodsReceiptStatus"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@empID", SqlDbType.Int, employeeID),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status)
        ]);
        conn.Execute(GetQuery("UpdateGoodsReceiptLineStatus"), [
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@Status", SqlDbType.Char, 1, (char)status)
        ]);
    }

    public List<GoodsReceiptReportAll> GetGoodsReceiptAllReport(int id) {
        var       data = new List<GoodsReceiptReportAll>();
        using var conn = Global.Connector;
        conn.ExecuteReader(GetQuery("GoodsReceiptAll"), new Parameter("@ID", SqlDbType.Int) { Value = id }, dr => {
            var line = new GoodsReceiptReportAll {
                ItemCode   = (string)dr["ItemCode"],
                ItemName   = dr["ItemName"].ToString(),
                Quantity   = Convert.ToInt32(dr["Quantity"]),
                Delivery   = Convert.ToInt32(dr["Delivery"]),
                Showroom   = Convert.ToInt32(dr["Showroom"]),
                Stock      = Convert.ToInt32(dr["OnHand"]),
                NumInBuy   = Convert.ToInt32(dr["NumInBuy"]),
                BuyUnitMsr = dr["BuyUnitMsr"].ToString(),
                PurPackUn   = Convert.ToInt32(dr["PurPackUn"]),
                PurPackMsr = dr["PurPackMsr"].ToString(),
            };
            data.Add(line);
        });
        return data;
    }

    public List<GoodsReceiptReportAllDetails> GetGoodsReceiptAllReportDetails(int id, string item) {
        var       data = new List<GoodsReceiptReportAllDetails>();
        using var conn = Global.Connector;
        conn.ExecuteReader(GetQuery("GoodsReceiptAllDetails"), [
                new Parameter("@ID", SqlDbType.Int) { Value                = id },
                new Parameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = item }
            ],
            dr => {
                int    lineID       = (int)dr["LineID"];
                string employeeName = (string)dr["EmployeeName"];
                var    timestamp    = (DateTime)dr["TimeStamp"];
                int    quantity     = Convert.ToInt32(dr["Quantity"]);

                var line = new GoodsReceiptReportAllDetails {
                    LineID       = lineID,
                    EmployeeName = employeeName,
                    TimeStamp    = timestamp,
                    Quantity     = quantity,
                    Unit = (UnitType)Convert.ToInt16(dr["Unit"])
                };
                data.Add(line);
            });
        return data;
    }

    public void UpdateGoodsReceiptAll(UpdateDetailParameters parameters, int employeeID) {
        using var conn = Global.Connector;
        try {
            parameters.RemoveRows?.ForEach(row => {
                var updateParameters = new UpdateLineParameter {
                    ID            = parameters.ID,
                    LineID        = row,
                    InternalClose = true,
                };
                UpdateLine(conn, updateParameters, employeeID);
            });
        }
        catch (Exception e) {
            conn.RollbackTransaction();
            throw new Exception("Remove Rows Error: " + e.Message);
        }

        if (parameters.QuantityChanges == null) {
            conn.CommitTransaction();
            return;
        }

        try {
            foreach (var updateLineParameter in parameters.QuantityChanges.Select(pair => new UpdateLineQuantityParameter {
                         ID       = parameters.ID,
                         LineID   = pair.Key,
                         Quantity = pair.Value
                     })) {
                UpdateLineQuantity(conn, updateLineParameter, employeeID);
            }
        }
        catch (Exception e) {
            conn.RollbackTransaction();
            throw new Exception("Update Quantity Error: " + e.Message);
        }

        conn.CommitTransaction();
    }

    public List<GoodsReceiptVSExitReport> GetGoodsReceiptVSExitReport(int id) {
        var       data    = new List<GoodsReceiptVSExitReport>();
        var       control = new Dictionary<(int, int), GoodsReceiptVSExitReport>();
        using var conn    = Global.Connector;
        conn.ExecuteReader(GetQuery("GoodsReceiptVSExit"), new Parameter("@ID", SqlDbType.Int) { Value = id }, dr => {
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

    public List<GoodsReceiptValidateProcess> GetGoodsReceiptValidateProcess(int id) {
        var       data    = new List<GoodsReceiptValidateProcess>();
        var       control = new Dictionary<(int, int), GoodsReceiptValidateProcess>();
        using var conn    = Global.Connector;
        conn.ExecuteReader(GetQuery("ValidateProcessGoodsReceiptLines"), new Parameter("@ID", SqlDbType.Int) { Value = id }, dr => {
            int baseType  = (int)dr["BaseType"];
            int baseEntry = (int)dr["BaseEntry"];
            var tuple     = (baseType, baseEntry);

            GoodsReceiptValidateProcess value;
            if (control.ContainsKey(tuple)) {
                value = control[tuple];
            }
            else {
                value = new GoodsReceiptValidateProcess {
                    DocumentNumber = (int)dr["DocNum"],
                    CardCode       = (string)dr["CardCode"],
                    CardName       = dr["CardName"].ToString(),
                    BaseType       = baseType,
                    BaseEntry      = baseEntry,
                };
                data.Add(value);
                control.Add(tuple, value);
            }

            var line = new GoodsReceiptValidateProcessLine {
                LineNumber = (int)dr["Visorder"] + 1,
                ItemCode   = (string)dr["ItemCode"],
                ItemName   = dr["ItemName"].ToString(),
                Quantity   = Convert.ToDecimal(dr["Quantity"]),
                BaseLine   = (int)dr["BaseLine"],
                OpenInvQty = Convert.ToDecimal(dr["OpenInvQty"]),
                PackUnit   = Convert.ToInt32(dr["PackUnit"]),
                BuyUnitMsr = dr["BuyUnitMsr"].ToString(),
                LineStatus = (GoodsReceiptValidateProcessLineStatus)dr["LineStatus"]
            };
            value.Lines.Add(line);
        });
        return data;
    }

    public List<GoodsReceiptValidateProcessLineDetails> GetGoodsReceiptValidateProcessLineDetails(GoodsReceiptValidateProcessLineDetailsParameters detailsParameters) {
        var       data = new List<GoodsReceiptValidateProcessLineDetails>();
        using var conn = Global.Connector;
        Parameters parameters = [
            new Parameter("@ID", SqlDbType.Int, detailsParameters.ID),
            new Parameter("@BaseType", SqlDbType.Int, detailsParameters.BaseType),
            new Parameter("@BaseEntry", SqlDbType.Int, detailsParameters.BaseEntry),
            new Parameter("@BaseLine", SqlDbType.Int, detailsParameters.BaseLine),
        ];
        conn.ExecuteReader(GetQuery("ValidateProcessGoodsReceiptLineDetails"), parameters, dr => {
            var value = new GoodsReceiptValidateProcessLineDetails();
            value.TimeStamp       = (DateTime)dr["TimeStamp"];
            value.Employee        = dr["EmployeeName"].ToString();
            value.Quantity        = Convert.ToDecimal(dr["Quantity"]);
            value.ScannedQuantity = Convert.ToDecimal(dr["ScannedQuantity"]);
            data.Add(value);
        });
        return data;
    }
}