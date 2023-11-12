using System;
using System.Data;

namespace Service.API.GoodsReceipt.Models;

public class GoodsReceiptCreationValue {
    public string ItemCode  { get; set; }
    public double Quantity  { get; set; }
    public string CardCode  { get; set; }
    public int    BaseType  { get; set; }
    public int    BaseEntry { get; set; }
    public int    BaseLine  { get; set; }

    public GoodsReceiptCreationValue(DataRow dr) {
        ItemCode  = (string)dr["ItemCode"];
        Quantity  = Convert.ToDouble(dr["Quantity"]);
        if (dr["CardCode"] != DBNull.Value)
            CardCode = (string)dr["CardCode"];
        BaseType = Convert.ToInt32(dr["BaseType"]);
        BaseEntry = Convert.ToInt32(dr["BaseEntry"]);
        BaseLine = Convert.ToInt32(dr["BaseLine"]);
    }
}