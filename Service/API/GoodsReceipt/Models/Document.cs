using System;
using System.Collections.Generic;
using Service.API.Models;

namespace Service.API.GoodsReceipt.Models;

public class Document {
    public int              ID              { get; set; }
    public string           Name            { get; set; }
    public DateTime         Date            { get; set; }
    public UserInfo         Employee        { get; set; }
    public DocumentStatus   Status          { get; set; }
    public DateTime         StatusDate      { get; set; }
    public UserInfo         StatusEmployee  { get; set; }
    public BusinessPartner  BusinessPartner { get; set; }
    public string           WhsCode         { get; set; }
    public int              GRPO            { get; set; }
    public GoodsReceiptType Type            { get; set; }
    public bool             Error           { get; set; }
    public int              ErrorCode       { get; set; }
    public object[]         ErrorParameters { get; set; }

    public List<DocumentParameter> SpecificDocuments { get; set; }
}