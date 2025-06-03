//
//     public bool ProcessTransfer(int id, int employeeID, List<string> sendTo) {
//         var transfer = GetTransfer(id);
//         if (transfer.Status != DocumentStatus.InProgress)
//             throw new Exception("Cannot process transfer if the Status is not In Progress");
//         UpdateTransferStatus(id, employeeID, DocumentStatus.Processing);
//         try {
//             using var creation = new TransferCreation(id, employeeID);
//             creation.Execute();
//             UpdateTransferStatus(id, employeeID, DocumentStatus.Finished);
//             creation.SetFinishedLines();
//             ProcessTransferSendAlert(id, sendTo, creation);
//             return true;
//         }
//         catch (Exception e) {
//             UpdateTransferStatus(id, employeeID, DocumentStatus.InProgress);
//             throw;
//         }
//     }
//
//     private void ProcessTransferSendAlert(int id, List<string> sendTo, TransferCreation creation) {
//         try {
//             using var alert = new Alert();
//             alert.Subject = string.Format(ErrorMessages.WMSTransactionAlert, id);
//             var transactionColumn = new AlertColumn(ErrorMessages.WMSTransaction);
//             var transferColumn    = new AlertColumn(ErrorMessages.InventoryTransfer, true);
//             alert.Columns.AddRange([transactionColumn, transferColumn]);
//             transactionColumn.Values.Add(new AlertValue(id.ToString()));
//             transferColumn.Values.Add(new AlertValue(creation.Number.ToString(), "67", creation.Entry.ToString()));
//
//             alert.Send(sendTo);
//         }
//         catch (Exception e) {
//             //todo log error handler
//         }
//     }
//
//     public IEnumerable<TransferContent> GetTransferContent(TransferContentParameters contentParameters) {
//         var        list        = new List<TransferContent>();
//         string     query       = $"TransferContent{contentParameters.Type.ToString()}";
//         Parameters queryParams = [new Parameter("@ID", SqlDbType.Int, contentParameters.ID)];
//         switch (contentParameters.Type) {
//             case SourceTarget.Source:
//                 queryParams.Add(new Parameter("@BinEntry", SqlDbType.Int, contentParameters.BinEntry > 0 ? contentParameters.BinEntry : DBNull.Value));
//                 break;
//             case SourceTarget.Target:
//                 queryParams.Add(new Parameter("@ItemCode", SqlDbType.NVarChar, !contentParameters.TargetBinQuantity && contentParameters.ItemCode != null ? contentParameters.ItemCode : DBNull.Value));
//                 queryParams.Add(new Parameter("@BinEntry", SqlDbType.Int, contentParameters.TargetBinQuantity ? contentParameters.BinEntry : DBNull.Value));
//                 break;
//         }
//
//         Dictionary<string, TransferContent> control = new();
//
//         using var conn = Global.Connector;
//         conn.ExecuteReader(GetQuery(query), queryParams, dr => {
//             var content = new TransferContent {
//                 Code       = (string)dr["ItemCode"],
//                 Name       = dr["ItemName"].ToString(),
//                 Quantity   = Convert.ToInt32(dr["Quantity"]),
//                 NumInBuy   = Convert.ToInt32(dr["NumInBuy"]),
//                 BuyUnitMsr = dr["BuyUnitMsr"].ToString(),
//                 PurPackUn  = Convert.ToInt32(dr["PurPackUn"]),
//                 PurPackMsr = dr["PurPackMsr"].ToString()
//             };
//             switch (contentParameters.Type) {
//                 case SourceTarget.Source:
//                     content.Unit       = (UnitType)Convert.ToInt16(dr["Unit"]);
//                     break;
//                 case SourceTarget.Target: {
//                     content.Progress     = Convert.ToInt32(dr["Progress"]);
//                     content.OpenQuantity = Convert.ToInt32(dr["OpenQuantity"]);
//                     if (contentParameters.TargetBinQuantity) {
//                         content.BinQuantity = Convert.ToInt32(dr["BinQuantity"]);
//                     }
//
//                     break;
//                 }
//             }
//
//             list.Add(content);
//             if (contentParameters.Type == SourceTarget.Target && contentParameters.TargetBins)
//                 control.Add(content.Code, content);
//         });
//
//         if (contentParameters.Type == SourceTarget.Target && contentParameters.TargetBins)
//             GetTransferContentBins(queryParams, control);
//
//         return list;
//     }
//
//     private static void GetTransferContentBins(Parameters queryParams, Dictionary<string, TransferContent> control) {
//         using var conn = Global.Connector;
//         conn.ExecuteReader(GetQuery("TransferContentTargetBins"), queryParams, dr => {
//             string itemCode = (string)dr["ItemCode"];
//             var bin = new TransferContentBin {
//                 Entry    = (int)dr["Entry"],
//                 Code     = (string)dr["Code"],
//                 Quantity = Convert.ToInt32(dr["Quantity"])
//             };
//             control[itemCode].Bins ??= [];
//             control[itemCode].Bins.Add(bin);
//         });
//     }
//
//     public IEnumerable<TransferContentTargetItemDetail> TransferContentTargetDetail(TransferContentTargetItemDetailParameters queryParams) {
//         var       data = new List<TransferContentTargetItemDetail>();
//         using var conn = Global.Connector;
//         conn.ExecuteReader(GetQuery("TransferContentTargetItemDetail"), [
//                 new Parameter("@ID", SqlDbType.Int) { Value                = queryParams.ID },
//                 new Parameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = queryParams.ItemCode },
//                 new Parameter("@BinEntry", SqlDbType.Int) { Value          = queryParams.BinEntry }
//             ],
//             dr => {
//                 int    lineID       = (int)dr["LineID"];
//                 string employeeName = (string)dr["EmployeeName"];
//                 var    timestamp    = (DateTime)dr["TimeStamp"];
//                 int    quantity     = Convert.ToInt32(dr["Quantity"]);
//
//                 var line = new TransferContentTargetItemDetail {
//                     LineID       = lineID,
//                     EmployeeName = employeeName,
//                     TimeStamp    = timestamp,
//                     Quantity     = quantity,
//                 };
//                 data.Add(line);
//             });
//         return data;
//     }
//
//     public int ValidateUpdateLine(DataConnector conn, UpdateLineParameter parameters) {
//         return conn.GetValue<int>(GetQuery("ValidateUpdateLineParameters"), [
//             new Parameter("@ID", SqlDbType.Int, parameters.ID),
//             new Parameter("@LineID", SqlDbType.Int, parameters.LineID),
//             new Parameter("@Reason", SqlDbType.Int, parameters.CloseReason.HasValue ? parameters.CloseReason.Value : DBNull.Value),
//             new Parameter("@Quantity", SqlDbType.Int, parameters.Quantity.HasValue ? parameters.Quantity.Value : DBNull.Value),
//         ]);
//     }
//
//     public void UpdateContentTargetDetail(UpdateDetailParameters parameters) {
//         using var conn = Global.Connector;
//         if (parameters.QuantityChanges != null) {
//             try {
//                 var control = new List<UpdateLineParameter>();
//                 foreach (var pair in parameters.QuantityChanges) {
//                     var updateLineParameter = new UpdateLineParameter {
//                         ID       = parameters.ID,
//                         LineID   = pair.Key,
//                         Quantity = pair.Value
//                     };
//                     var isValid = (UpdateLineReturnValue)ValidateUpdateLine(conn, updateLineParameter);
//                     switch (isValid) {
//                         case UpdateLineReturnValue.Status:
//                             throw new Exception($"Transfer status is not In Progress");
//                         case UpdateLineReturnValue.LineStatus:
//                             throw new Exception($"Trans Line Status is not In Progress");
//                         case UpdateLineReturnValue.QuantityMoreThenAvailable:
//                             throw new Exception(ErrorMessages.QuantityMoreThenAvailableCurrent);
//                     }
//
//                     control.Add(updateLineParameter);
//                 }
//
//                 control.ForEach(updateLineParameter => UpdateLine(conn, updateLineParameter));
//             }
//             catch (Exception e) {
//                 conn.RollbackTransaction();
//                 throw new Exception($"Update Quantity Error: {e.Message}");
//             }
//         }
//
//         try {
//             parameters.RemoveRows?.ForEach(row => {
//                 UpdateLine(conn, new UpdateLineParameter {
//                     ID            = parameters.ID,
//                     LineID        = row,
//                     InternalClose = true,
//                 });
//             });
//         }
//         catch (Exception e) {
//             conn.RollbackTransaction();
//             throw new Exception("Remove Rows Error: " + e.Message);
//         }
//
//         conn.CommitTransaction();
//     }
//
//     public static string GetQuery(string id) {
//         string resourceName = $"Service.API.Transfer.Queries.{ConnectionController.DatabaseType}.{id}.sql";
//         var    assembly     = typeof(Queries).Assembly;
//         string resourcePath = resourceName;
//
//         using var stream = assembly.GetManifestResourceStream(resourcePath);
//         if (stream == null) {
//             throw new ArgumentException($"Specified resource not found: {resourceName}");
//         }
//
//         using var reader = new StreamReader(stream);
//         return reader.ReadToEnd();
//     }
//
//     private static void UpdateTransferStatus(int id, int employeeID, DocumentStatus status) {
//         using var conn = Global.Connector;
//         conn.Execute(GetQuery("UpdateTransferStatus"), [
//             new Parameter("@ID", SqlDbType.Int, id),
//             new Parameter("@empID", SqlDbType.Int, employeeID),
//             new Parameter("@Status", SqlDbType.Char, 1, (char)status)
//         ]);
//         conn.Execute(GetQuery("UpdateTransferLineStatus"), [
//             new Parameter("@ID", SqlDbType.Int, id),
//             new Parameter("@Status", SqlDbType.Char, 1, (char)status)
//         ]);
//     }
//
//     public int CreateTransferRequest(TransferContent[] contents, EmployeeData employee) {
//         using var creation = new TransferRequestCreation(contents, employee);
//         creation.Execute();
//         return creation.Number;
//     }
// }