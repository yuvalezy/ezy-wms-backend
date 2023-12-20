using System;
using System.Collections.Generic;
using System.Data;

namespace Service.API.Picking.Models;

public class PickingDocument {
    public int        Entry          { get; set; }
    public DateTime   Date           { get; set; }
    public int        SalesOrders    { get; set; }
    public int        Invoices       { get; set; }
    public int        Transfers      { get; set; }
    public string     Remarks        { get; set; }
    public PickStatus Status         { get; set; }
    public int        Quantity       { get; set; }
    public int        OpenQuantity   { get; set; }
    public int        UpdateQuantity { get; set; }

    public List<PickingDocumentDetail> Detail { get; set; }

    public static PickingDocument Read(IDataReader dr) {
        var pick = new PickingDocument();
        pick.Entry          = (int)dr["AbsEntry"];
        pick.Date           = (DateTime)dr["PickDate"];
        pick.SalesOrders    = (int)dr["SalesOrders"];
        pick.Invoices       = (int)dr["Invoices"];
        pick.Transfers      = (int)dr["Transfers"];
        pick.Status         = (PickStatus)Convert.ToChar(dr["Status"]);
        pick.Quantity       = Convert.ToInt32(dr["Quantity"]);
        pick.OpenQuantity   = Convert.ToInt32(dr["OpenQuantity"]);
        pick.UpdateQuantity = Convert.ToInt32(dr["UpdateQuantity"]);
        if (dr["Remarks"] != DBNull.Value)
            pick.Remarks = (string)dr["Remarks"];
        return pick;
    }
}

public class PickingDocumentDetail {
    public int                             Type           { get; set; }
    public int                             Entry          { get; set; }
    public int                             Number         { get; set; }
    public DateTime                        Date           { get; set; }
    public string                          CardCode       { get; set; }
    public string                          CardName       { get; set; }
    public List<PickingDocumentDetailItem> Items          { get; set; }
    public int                             TotalItems     { get; set; }
    public int                             TotalOpenItems { get; set; }

    public static PickingDocumentDetail Read(IDataReader dr) {
        var detail = new PickingDocumentDetail();
        detail.Type           = (int)dr["Type"];
        detail.Entry          = (int)dr["Entry"];
        detail.Number         = (int)dr["DocNum"];
        detail.Date           = (DateTime)dr["DocDate"];
        detail.CardCode       = dr["CardCode"].ToString();
        detail.CardName       = dr["CardName"].ToString();
        detail.TotalItems     = Convert.ToInt32(dr["TotalItems"]);
        detail.TotalOpenItems = Convert.ToInt32(dr["TotalOpenItems"]);
        return detail;
    }
}

public class PickingDocumentDetailItem {
    public string ItemCode     { get; set; }
    public string ItemName     { get; set; }
    public int    Quantity     { get; set; }
    public int    Picked       { get; set; }
    public int    OpenQuantity { get; set; }

    public static PickingDocumentDetailItem Read(IDataReader dr) {
        var item = new PickingDocumentDetailItem();
        item.ItemCode     = (string)dr["ItemCode"];
        item.ItemName     = dr["ItemName"].ToString();
        item.Quantity     = Convert.ToInt32(dr["Quantity"]);
        item.Picked       = Convert.ToInt32(dr["Picked"]);
        item.OpenQuantity = Convert.ToInt32(dr["OpenQuantity"]);
        return item;
    }
}