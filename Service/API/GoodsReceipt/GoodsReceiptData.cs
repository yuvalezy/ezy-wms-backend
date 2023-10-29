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
using Alert = Service.API.General.Alert;

namespace Service.API.GoodsReceipt;

public class GoodsReceiptData {
    public bool CancelDocument(int id, int employeeID) {
        var doc = GetDocument(id);
        if (doc.Status is not (DocumentStatus.Open or DocumentStatus.InProgress))
            throw new Exception("Cannot cancel document if the Status is not Open or In Progress");
        UpdateDocumentStatus(id, employeeID, DocumentStatus.Cancelled);
        return true;
    }

    public bool ProcessDocument(int id, int employeeID, List<string> sendTo) {
        var doc = GetDocument(id);
        if (doc.Status != DocumentStatus.InProgress)
            throw new Exception("Cannot process document if the Status is not In Progress");
        UpdateDocumentStatus(id, employeeID, DocumentStatus.Processing);
        try {
            using var creation = new GoodsReceiptCreation(id, employeeID);
            creation.Execute();
            UpdateDocumentStatus(id, employeeID, DocumentStatus.Finished);
            using var alert = new Alert();
            alert.Subject = string.Format(ErrorMessages.WMSTransactionAlert, id);
            alert.Columns.Add(new AlertColumn(ErrorMessages.WMSTransaction) {
                Values = new List<AlertValue> {
                    new(id.ToString())
                }
            });
            alert.Columns.Add(new AlertColumn(ErrorMessages.DraftNumber, true) {
                Values = new List<AlertValue> {
                    new(creation.NewEntry.ToString(), "112", creation.NewEntry.ToString())
                }
            });

            alert.Send(sendTo);
            return true;
        }
        catch (Exception e) {
            UpdateDocumentStatus(id, employeeID, DocumentStatus.InProgress);
            throw;
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
        int id = Global.DataObject.GetValue<int>(GetQuery("CreateGoodsReceipt"), @params);

        if (createParameters.Type != GoodsReceiptType.SpecificOrders)
            return id;

        string query = GetQuery("CreateGoodsReceiptDocument");
        @params = new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@ObjType", SqlDbType.Int),
            new Parameter("@DocEntry", SqlDbType.Int),
        };
        createParameters.Documents.ForEach(value => {
            @params["@ObjType"].Value  = value.ObjectType;
            @params["@DocEntry"].Value = value.DocumentNumber;
            Global.DataObject.Execute(query, @params);
        });

        return id;
    }

    public int ValidateUpdateLine(UpdateLineParameter parameters) =>
        Global.DataObject.GetValue<int>(GetQuery("ValidateUpdateLineParameters"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, parameters.ID),
            new Parameter("@LineID", SqlDbType.Int, parameters.LineID),
            new Parameter("@Reason", SqlDbType.Int, parameters.CloseReason.HasValue ? parameters.CloseReason.Value : DBNull.Value),
        });

    public void UpdateLine(UpdateLineParameter updateLineParameter, int empID) {
        var parameters = new Parameters {
            new Parameter("@ID", SqlDbType.Int) { Value     = updateLineParameter.ID },
            new Parameter("@LineID", SqlDbType.Int) { Value = updateLineParameter.LineID },
        };
        var  sb       = new StringBuilder("update \"@LW_YUVAL08_GRPO1\" set ");
        bool comma    = false;
        bool userSign = false;
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
            comma    = true;
            userSign = true;
        }

        if (updateLineParameter.QuantityInUnit.HasValue) {
            if (comma)
                sb.AppendLine(", ");
            sb.AppendLine("\"U_QtyPerUnit\" = @QuantityInUnit ");
            parameters.Add(new Parameter("@QuantityInUnit", SqlDbType.Int) { Value = updateLineParameter.QuantityInUnit.Value });
            comma    = true;
            userSign = true;
        }

        if (userSign) {
            sb.AppendLine(", \"U_StatusUserSign\" = @UserSign, \"U_StatusTimeStamp\" = getdate() ");
            parameters.Add(new Parameter("@UserSign", SqlDbType.Int) { Value = empID });
        }

        sb.AppendLine("where U_ID = @ID and \"U_LineID\" = @LineID");

        Global.DataObject.Execute(sb.ToString(), parameters);

        if (updateLineParameter.CloseReason.HasValue) {
            Global.DataObject.Execute("update \"@LW_YUVAL08_GRPO2\" set \"U_TargetStatus\" = 'C' where U_ID = @ID and U_LineID = @LineID", new Parameters {
                new Parameter("@ID", SqlDbType.Int) { Value     = updateLineParameter.ID },
                new Parameter("@LineID", SqlDbType.Int) { Value = updateLineParameter.LineID },
            });
        }
    }

    public int ValidateAddItem(int id, string itemCode, string barCode) =>
        Global.DataObject.GetValue<int>(GetQuery("ValidateAddItemParameters"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@ItemCode", SqlDbType.NVarChar, 50, itemCode),
            new Parameter("@BarCode", SqlDbType.NVarChar, 254, barCode),
        });

    public AddItemResponse AddItem(int id, string itemCode, string barcode, int employeeID) {
        AddItemResponse returnValue = null;
        try {
            Global.DataObject.BeginTransaction();
            Global.DataObject.ExecuteReader(GetQuery("AddItem"), new Parameters {
                new Parameter("@ID", SqlDbType.Int, id),
                new Parameter("@ItemCode", SqlDbType.NVarChar, 50, itemCode),
                new Parameter("@BarCode", SqlDbType.NVarChar, 254, barcode),
                new Parameter("@empID", SqlDbType.Int, employeeID),
            }, dr => {
                returnValue = new AddItemResponse() {
                    LineID      = (int)dr["LineID"],
                    Fulfillment = (int)dr["Fulfillment"] > 0,
                    Showroom    = (int)dr["Showroom"] > 0,
                    Warehouse   = (int)dr["Warehouse"] > 0,
                    NumInBuy    = (int)dr["NumInBuy"]
                };
            });
            if (returnValue == null)
                throw new Exception("Add Item Result Empty!");
            Global.DataObject.CommitTransaction();
        }
        catch {
            Global.DataObject.RollbackTransaction();
            throw;
        }

        return returnValue;
    }

    public Document GetDocument(int id) {
        Document doc = null;
        var      sb  = new StringBuilder(GetQuery("GetGoodsReceipts"));
        sb.Append(" where DOCS.\"Code\" = @ID");
        Global.DataObject.ExecuteReader(sb, new Parameter("@ID", SqlDbType.Int, id), dr => doc = ReadDocument(dr));
        GetDocumentsSpecificDocuments(doc);
        return doc;
    }

    public Document[] GetDocuments(FilterParameters parameters) {
        List<Document> docs = new();
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
        var documents = docs.ToArray();
        GetDocumentsSpecificDocuments(documents);
        return documents;
    }

    private static Document ReadDocument(IDataReader dr) {
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
        if (dr["GRPO"] != DBNull.Value)
            doc.GRPO = (int)dr["GRPO"];
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
        using var dt = Global.DataObject.GetDataTable(query);
        using var dv = new DataView(dt);
        foreach (var document in filtered) {
            dv.RowFilter = $"ID = {document.ID}";
            var specific = new List<DocumentParameter>();
            document.SpecificDocuments = specific;
            specific.AddRange(
                from DataRowView dvr
                    in dv
                select new DocumentParameter {
                    ObjectType = (int)dvr["ObjType"], DocumentNumber = (int)dvr["DocNum"],
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

    public List<GoodsReceiptReportAll> GetGoodsReceiptAllReport(int id) {
        var data = new List<GoodsReceiptReportAll>();
        Global.DataObject.ExecuteReader(GetQuery("GoodsReceiptAll"), new Parameter("@ID", SqlDbType.Int) { Value = id }, dr => {
            string itemCode = (string)dr["ItemCode"];
            string itemName = dr["ItemName"].ToString();
            int    quantity = Convert.ToInt32(dr["Quantity"]);
            int    delivery = Convert.ToInt32(dr["Delivery"]);
            int    showroom = Convert.ToInt32(dr["Showroom"]);
            int    stock    = Convert.ToInt32(dr["OnHand"]);

            var line = new GoodsReceiptReportAll {
                ItemCode = itemCode,
                ItemName = itemName,
                Quantity = quantity,
                Delivery = delivery,
                Showroom = showroom,
                Stock    = stock
            };
            data.Add(line);
        });
        return data;
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