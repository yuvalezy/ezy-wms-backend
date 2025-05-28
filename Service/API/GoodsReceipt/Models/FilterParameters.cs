using System;
using Service.API.Models;

namespace Service.API.GoodsReceipt.Models;

public class FilterParameters {
    internal string WhsCode { get; set; }

    public DateTime?        Date            { get; set; }
    public DateTime?        DateFrom        { get; set; }
    public DateTime?        DateTo          { get; set; }
    public string           BusinessPartner { get; set; }
    public string           Name            { get; set; }
    public DocumentStatus[] Status          { get; set; }
    public OrderBy?         OrderBy         { get; set; }
    public int?             ID              { get; set; }
    public int?             GRPO            { get; set; }
    public int?             PurchaseOrder   { get; set; }
    public int?             ReservedInvoice { get; set; }
    public int?             GoodsReceipt    { get; set; }
    public int?             PurchaseInvoice { get; set; }
    public bool?            OrderByDesc     { get; set; }
    public int?             LastID          { get; set; }
    public int?             PageSize        { get; set; }
    public int?             PageNumber      { get; set; }
    public bool?            Confirm         { get; set; }
}