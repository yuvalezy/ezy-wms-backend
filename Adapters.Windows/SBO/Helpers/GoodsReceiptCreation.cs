using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.DTOs.GoodsReceipt;
using Core.Models;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class GoodsReceiptCreation(SboCompany sboCompany, int number, string whsCode, int series, Dictionary<string, List<GoodsReceiptCreationDataResponse>> data)
    : IDisposable {
    private          Documents?                                                 doc;
    private          Recordset?                                                 rs;

    private List<(int Entry, int Number)> NewEntries { get; } = [];

    public ProcessGoodsReceiptResult Execute() {
        try {
            if (!sboCompany.TransactionMutex.WaitOne())
                return new ProcessGoodsReceiptResult {
                    Success = false,
                    ErrorMessage = "Unable to acquire transaction mutex"
                };

            try {
                sboCompany.ConnectCompany();
                sboCompany.Company.StartTransaction();

                // Group data by source documents for batch creation
                var groupedData = GroupDataBySource();

                foreach (var group in groupedData) {
                    CreateDocument(group.Key, group.Value);
                }

                if (sboCompany.Company.InTransaction)
                    sboCompany.Company.EndTransaction(BoWfTransOpt.wf_Commit);

                return new ProcessGoodsReceiptResult {
                    Success = true,
                    DocumentNumber = NewEntries.FirstOrDefault().Number
                };
            }
            finally {
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception ex) {
            if (sboCompany.Company.InTransaction)
                sboCompany.Company.EndTransaction(BoWfTransOpt.wf_RollBack);

            return new ProcessGoodsReceiptResult {
                Success = false,
                ErrorMessage = $"Error generating Goods Receipt: {ex.Message}"
            };
        }
    }

    private Dictionary<(string? CardCode, int Type, int Entry), List<GoodsReceiptCreationDataResponse>> GroupDataBySource() {
        var grouped = new Dictionary<(string? CardCode, int Type, int Entry), List<GoodsReceiptCreationDataResponse>>();

        foreach (var item in data.SelectMany(kvp => kvp.Value)) {
            if (item.Sources.Any()) {
                foreach (var source in item.Sources) {
                    var key = (CardCode: (string?)null, source.SourceType, source.SourceEntry);
                    if (!grouped.ContainsKey(key))
                        grouped[key] = new List<GoodsReceiptCreationDataResponse>();
                    grouped[key].Add(item);
                }
            }
            else {
                // Items without sources go into a general receipt
                var key = (CardCode: (string?)null, Type: 0, Entry: 0);
                if (!grouped.ContainsKey(key))
                    grouped[key] = new List<GoodsReceiptCreationDataResponse>();
                grouped[key].Add(item);
            }
        }

        return grouped;
    }

    private void CreateDocument((string? CardCode, int Type, int Entry) key, List<GoodsReceiptCreationDataResponse> items) {
        doc = (Documents)sboCompany.Company.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);
        try {
            // Set document header
            doc.DocDate = DateTime.Now;
            doc.TaxDate = DateTime.Now;
            doc.Series = series;
            
            if (!string.IsNullOrEmpty(key.CardCode))
                doc.CardCode = key.CardCode;

            doc.UserFields.Fields.Item("U_LW_GR_NUMBER").Value = number.ToString();
            doc.Comments = $"Generated from WMS Goods Receipt #{number}";

            // Add lines
            foreach (var item in items) {
                doc.Lines.ItemCode = item.ItemCode;
                doc.Lines.Quantity = (double)item.Quantity;
                doc.Lines.WarehouseCode = whsCode;
                
                if (!string.IsNullOrEmpty(item.Comments))
                    doc.Lines.FreeText = item.Comments;

                // Link to source document if applicable
                if (item.Sources.Any()) {
                    var source = item.Sources.First();
                    doc.Lines.BaseType = source.SourceType;
                    doc.Lines.BaseEntry = source.SourceEntry;
                    doc.Lines.BaseLine = source.SourceLine;
                }

                doc.Lines.Add();
            }

            // Create the document
            if (doc.Add() != 0) {
                throw new Exception(sboCompany.Company.GetLastErrorDescription());
            }

            // Get the created document number
            string newEntry = sboCompany.Company.GetNewObjectKey();
            rs = (Recordset)sboCompany.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            rs.DoQuery($"SELECT DocNum FROM OPDN WHERE DocEntry = {newEntry}");
            if (!rs.EoF) {
                int docNum = (int)rs.Fields.Item("DocNum").Value;
                NewEntries.Add((int.Parse(newEntry), docNum));
            }
        }
        finally {
            if (doc != null) {
                Marshal.ReleaseComObject(doc);
                doc = null;
            }
        }
    }

    public void Dispose() {
        if (rs != null) {
            Marshal.ReleaseComObject(rs);
            rs = null;
        }
        if (doc != null) {
            Marshal.ReleaseComObject(doc);
            doc = null;
        }
        GC.Collect();
    }
}